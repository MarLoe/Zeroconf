using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Heijden.DNS;
using Type = Heijden.DNS.Type;

namespace Zeroconf
{
    /// <summary>
    ///     Looks for ZeroConf devices
    /// </summary>
    public static partial class ZeroconfResolver
    {
        private static readonly AsyncLock ResolverLock = new();

        private static readonly INetworkInterface NetworkInterface = new NetworkInterface();

        private static IEnumerable<string> BrowseResponseParser(Response response)
        {
            return response.RecordsPTR.Select(ptr => ptr.PTRDNAME);
        }

        private static async Task<IDictionary<string, Response>> ResolveInternal(
            ZeroconfOptions options,
            Action<string, Response> callback,
            CancellationToken cancellationToken,
            System.Net.NetworkInformation.NetworkInterface[] netInterfacesToSendRequestOn = null)
        {
            var requestBytes = GetRequestBytes(options);

            using (options.AllowOverlappedQueries ? Disposable.Empty : await ResolverLock.LockAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dict = new Dictionary<string, Response>();

                void Converter(IPAddress address, byte[] buffer)
                {
                    var resp = new Response(buffer);
                    if (resp.IsQueryResponse)
                    {
                        var firstPtr = MatchRecord(resp, options);
                        if (firstPtr is not null)
                        {
                            var name = GetDisplayName(firstPtr);
                            if (string.IsNullOrEmpty(name))
                            {
                                return;
                            }

                            Debug.WriteLine($"IP: {address}, {(string.IsNullOrEmpty(name) ? string.Empty : $"Name: {name}, ")}Bytes: {buffer.Length}, IsResponse: {resp.header.QR}");

                            var key = $"{address}:{name}";
                            lock (dict)
                            {
                                dict[key] = resp;
                            }

                            callback?.Invoke(key, resp);
                        }
                    }
                }

                Debug.WriteLine($"Looking for {string.Join(", ", options.Protocols)} with scantime {options.ScanTime}");

                await NetworkInterface.NetworkRequestAsync(requestBytes,
                                                           options.ScanTime,
                                                           options.Retries,
                                                           (int)options.RetryDelay.TotalMilliseconds,
                                                           Converter,
                                                           cancellationToken,
                                                           netInterfacesToSendRequestOn)
                                      .ConfigureAwait(false);

                return dict;
            }
        }

        private static byte[] GetRequestBytes(ZeroconfOptions options)
        {
            var queryType = options.ScanQueryType is ScanQueryType.Ptr ? QType.PTR : QType.ANY;
            var req = new Request(options.Protocols.Select(p => new Question(p, queryType, QClass.IN)));
            return req.Data;
        }

        private static ZeroconfHost ResponseToZeroconf(Response response, string remoteAddress, ResolveOptions options)
        {
            var ipv4Adresses = response.RecordsRR
                                      .Select(r => r.RECORD)
                                      .OfType<RecordA>()
                                      .Concat(response.Additionals
                                                      .Select(r => r.RECORD)
                                                      .OfType<RecordA>())
                                      .Select(aRecord => aRecord.Address)
                                      .Distinct()
                                      .ToList();
            if (!ipv4Adresses.Any())
            {
                var address = remoteAddress.Split(':').FirstOrDefault();
                if (!string.IsNullOrEmpty(address))
                {
                    ipv4Adresses.Add(address);
                }
            }

            var ipv6Adresses = response.RecordsRR
                                      .Select(r => r.RECORD)
                                      .OfType<RecordAAAA>()
                                      .Concat(response.Additionals
                                                      .Select(r => r.RECORD)
                                                      .OfType<RecordAAAA>())
                                      .Select(aRecord => aRecord.Address)
                                      .Distinct()
                                      .ToList();

            var ptrDomains = response.RecordsPTR.Select(r => r.PTRDNAME).ToList();
            if (!ptrDomains.Any() && options is not null)
            {
                
                // The response did not contain any PTR records.
                // Let's use the domains we are looking for instead.
                ptrDomains = options.Protocols.ToList();
            }

            var z = new ZeroconfHost
            {
                Id = ipv4Adresses.FirstOrDefault() ?? remoteAddress,
                DisplayName = GetDisplayName(response, options),
                Hostname = GetHostname(response, ptrDomains),
                IPAddresses = ipv4Adresses.Concat(ipv6Adresses).ToList(),
                Domains = ptrDomains,
            };


            foreach (var ptr in ptrDomains)
            {
                // Get the matching service records
                var responseRecords = response.RecordsRR
                                             .Where(r => ptr.Equals(r.NAME, StringComparison.InvariantCultureIgnoreCase))
                                             .Select(r => r.RECORD)
                                             .ToList();

                var ptrRec = response.RecordsPTR.FirstOrDefault(p => p.PTRDNAME.Equals(ptr, StringComparison.InvariantCultureIgnoreCase));

                foreach (var srvRec in responseRecords.OfType<RecordSRV>())
                {
                    var svc = new Service
                    {
                        Name = ptrRec?.RR.NAME ?? ptr,
                        ServiceName = srvRec.RR.NAME,
                        Port = srvRec.PORT,
                        Ttl = (int)srvRec.RR.TTL,
                    };

                    // There may be 0 or more text records - property sets
                    foreach (var txtRec in responseRecords.OfType<RecordTXT>())
                    {
                        var set = new Dictionary<string, string>();
                        foreach (var txt in txtRec.TXT)
                        {
                            var split = txt.Split(new[] { '=' }, 2);
                            if (split.Length == 1)
                            {
                                if (!string.IsNullOrWhiteSpace(split[0]))
                                    set[split[0]] = null;
                            }
                            else
                            {
                                set[split[0]] = split[1];
                            }
                        }
                        svc.AddPropertySet(set);
                    }

                    z.AddService(svc);
                }
            }

            return z;
        }

        private static RR MatchRecord(Response response, ZeroconfOptions options)
        {
            return response.RecordsRR.FirstOrDefault(rr => options.Protocols.Any(p => rr.NAME.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)));
        }

        private static string GetHostname(Response response, List<string> ptrDomains)
        {
            // Find hostname from A Record (if available)
            var hostname = response.RecordsRR.FirstOrDefault(rr => rr.Type is Type.A)?.NAME;
            if (string.IsNullOrEmpty(hostname))
            {
                // Could not find hostname - look at services
                var records = response.RecordsRR.AsEnumerable();
                if (ptrDomains.Any())
                {
                    records = records.Where(r => ptrDomains.Contains(r.NAME, StringComparer.InvariantCultureIgnoreCase));
                }
                hostname = records
                    .Select(r => r.RECORD)
                    .OfType<RecordSRV>()
                    .FirstOrDefault()?.TARGET;
            }

            return hostname;
        }

        private static string GetDisplayName(Response response, ResolveOptions options)
        {
            if (options is not null)
            {
                var recPtr = response.RecordsPTR.FirstOrDefault(r => options.Protocols.Contains(r.RR.NAME, StringComparer.InvariantCultureIgnoreCase));
                if (recPtr is not null)
                {
                    return GetDisplayName(recPtr);
                }
            }
            return GetDisplayName(response.RecordsPTR.FirstOrDefault());
        }

        private static string GetDisplayName(RR rr)
        {
            if (rr.RECORD is RecordPTR recPtr)
            {
                return GetDisplayName(recPtr);
            }
            return rr?.NAME;
        }

        private static string GetDisplayName(RecordPTR ptrRec)
        {
            return ptrRec?.PTRDNAME.Replace(ptrRec.RR.NAME, "").TrimEnd('.');
        }
    }
}

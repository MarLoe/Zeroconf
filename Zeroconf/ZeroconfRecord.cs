using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zeroconf
{
    /// <summary>
    ///     A ZeroConf record response
    /// </summary>
    public interface IZeroconfHost
    {
        /// <summary>
        ///     Name
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        ///     Id, possibly different than the Name
        /// </summary>
        string Id { get; }

        /// <summary>
        ///     IP Address (alias for IPAddresses.First())
        /// </summary>
        string IPAddress { get; }

        /// <summary>
        ///     IP Addresses
        /// </summary>
        IReadOnlyList<string> IPAddresses { get; }


        /// <summary>
        ///     Services offered by this host (based on services queried for)
        /// </summary>
        IReadOnlyDictionary<string, IService> Services { get; }
    }

    /// <summary>
    ///     Represents a service provided by a host
    /// </summary>
    public interface IService
    {
        /// <summary>
        ///     This is the name retrieved from the PTR record
        /// e.g. _http._tcp.local.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     This is the name retrieved from the SRV record e.g. myserver._http._tcp.local.
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        ///     Port
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Time-to-live
        /// </summary>
        int Ttl { get; }

        /// <summary>
        ///     Properties of the object. Most services have a single set of properties, but some services
        ///     may return multiple sets of properties
        /// </summary>
        IReadOnlyList<IReadOnlyDictionary<string, string>> Properties { get; }
    }

    /// <summary>
    ///     A ZeroConf record response
    /// </summary>
    internal class ZeroconfHost : IZeroconfHost, IEquatable<ZeroconfHost>, IEquatable<IZeroconfHost>
    {
        private readonly Dictionary<string, IService> services = new();

        /// <summary>
        ///     Id, possibly different than the display name
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     IP Address (alias for IPAddresses.First())
        /// </summary>
        public string IPAddress => IPAddresses?.FirstOrDefault();

        /// <summary>
        ///     IP Addresses
        /// </summary>
        public IReadOnlyList<string> IPAddresses { get; set; }

        /// <summary>
        ///     Collection of services provided by the host
        /// </summary>
        public IReadOnlyDictionary<string, IService> Services => services;

        /// <summary>
        ///     Display Name
        /// </summary>
        public string DisplayName { get; set; }

        public bool Equals(IZeroconfHost other)
        {
            return Equals(other as ZeroconfHost);
        }

        public bool Equals(ZeroconfHost other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Id, other.Id) && string.Equals(IPAddress, other.IPAddress);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ZeroconfHost)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var addressesHash = IPAddresses?.Aggregate(0, (current, address) => (current * 397) ^ address.GetHashCode()) ?? 0;
                return ((Id != null ? Id.GetHashCode() : 0) * 397) ^ addressesHash;
            }
        }

        /// <summary>
        ///     Diagnostic
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"| ----------------------------------------------");
            sb.AppendLine($"| HOST");
            sb.AppendLine($"| ----------------------------------------------");
            sb.AppendLine($"| Id: {Id}");
            sb.AppendLine($"| DisplayName: {DisplayName}");
            sb.AppendLine($"| IPs: {string.Join(", ", IPAddresses)}");
            sb.AppendLine($"| Services: {services.Count}");

            if (services.Any())
            {
                var i = 0;
                foreach (var service in services)
                {
                    sb.AppendLine("\t| -------------------");
                    sb.AppendLine($"\t| Service #{i++}");
                    sb.AppendLine("\t| -------------------");
                    sb.AppendLine(service.Value.ToString());
                    sb.AppendLine("\t| -------------------");
                }
            }

            sb.AppendLine($"| ----------------------------------------------");

            return sb.ToString();
        }

        internal void AddService(IService service)
        {
            if (service is null) {
                throw new ArgumentNullException(nameof(service));
            }
            if (service.ServiceName is null) {
                throw new ArgumentNullException(nameof(service.ServiceName));
            }
            services[service.ServiceName] = service;
        }
    }

    internal class Service : IService
    {
        private readonly List<IReadOnlyDictionary<string, string>> properties = new();

        public string Name { get; set; }
        public string ServiceName { get; set; }
        public int Port { get; set; }
        public int Ttl { get; set; }

        public IReadOnlyList<IReadOnlyDictionary<string, string>> Properties => properties;

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"\t| Service: {Name}");
            sb.AppendLine($"\t| ServiceName: {ServiceName}");
            sb.AppendLine($"\t| Port: {Port}");
            sb.AppendLine($"\t| TTL: {Ttl}");
            sb.AppendLine($"\t| PropertySets: {properties.Count}");

            if (properties.Any())
            {
                var i = 0;
                foreach (var property in properties)
                {
                    sb.AppendLine($"\t\t| -------------------");
                    sb.AppendLine($"\t\t| Property Set #{i++}");
                    sb.AppendLine($"\t\t| -------------------");

                    foreach (var kvp in property)
                    {
                        sb.AppendLine($"\t\t| {kvp.Key} = {kvp.Value}");
                    }
                    sb.AppendLine($"\t\t| -------------------");
                }
            }

            return sb.ToString();
        }

        internal void AddPropertySet(IReadOnlyDictionary<string, string> set)
        {
            properties.Add(set ?? throw new ArgumentNullException(nameof(set)));
        }

    }
}

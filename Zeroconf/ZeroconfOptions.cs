﻿using System;
using System.Collections.Generic;

namespace Zeroconf
{
    public abstract class ZeroconfOptions
    {
        int retries;

        protected ZeroconfOptions(string protocol) :
            this(new[] { protocol })
        {
        }

        protected ZeroconfOptions(IEnumerable<string> protocols)
        {
            Protocols = new HashSet<string>(protocols ?? throw new ArgumentNullException(nameof(protocols)), StringComparer.OrdinalIgnoreCase);

            Retries = 2;
            RetryDelay = TimeSpan.FromSeconds(2);
            ScanTime = TimeSpan.FromSeconds(2);
            ScanQueryType = ScanQueryType.Ptr;
        }

        public IEnumerable<string> Protocols { get; }

        public int Retries
        {
            get => retries;
            set => retries = value is >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        public TimeSpan RetryDelay { get; set; }

        public TimeSpan ScanTime { get; set; }

        public ScanQueryType ScanQueryType { get; set; }

        public bool AllowOverlappedQueries { get; set; }
    }

    public enum ScanQueryType
    {
        Ptr,
        Any
    }

    public class BrowseDomainsOptions : ZeroconfOptions
    {
        public BrowseDomainsOptions() : base("_services._dns-sd._udp.local.")
        {
        }
    }

    public class ResolveOptions : ZeroconfOptions
    {
        public ResolveOptions(string protocol) : base(protocol)
        {
        }

        public ResolveOptions(IEnumerable<string> protocols) : base(protocols)
        {
        }
    }
}
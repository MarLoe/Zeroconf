﻿using System;

namespace Zeroconf
{
    public readonly struct ServiceAnnouncement : IEquatable<ServiceAnnouncement>
    {
        public bool Equals(ServiceAnnouncement other)
        {
            return AdapterInformation.Equals(other.AdapterInformation) && Equals(Host, other.Host);
        }

        public override bool Equals(object obj)
        {
            return obj is ServiceAnnouncement announcement && Equals(announcement);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (AdapterInformation.GetHashCode()*397) ^ (Host != null ? Host.GetHashCode() : 0);
            }
        }

        public static bool operator ==(ServiceAnnouncement left, ServiceAnnouncement right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ServiceAnnouncement left, ServiceAnnouncement right)
        {
            return !left.Equals(right);
        }

        public ServiceAnnouncement(AdapterInformation adapterInformation, IZeroconfHost host)
        {
            AdapterInformation = adapterInformation;
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public AdapterInformation AdapterInformation { get; }

        public IZeroconfHost Host { get; }
    }
}

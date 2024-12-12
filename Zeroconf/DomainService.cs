using System;

namespace Zeroconf
{
    public readonly struct DomainService : IEquatable<DomainService>
    {
        public bool Equals(DomainService other)
        {
            return string.Equals(Domain, other.Domain) && string.Equals(Service, other.Service);
        }

        public override bool Equals(object obj)
        {
            return obj is DomainService service && Equals(service);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Domain?.GetHashCode() ?? 0) * 397) ^ (Service?.GetHashCode() ?? 0);
            }
        }

        public static bool operator ==(DomainService left, DomainService right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DomainService left, DomainService right)
        {
            return !left.Equals(right);
        }

        public DomainService(string domain, string service)
        {
            Domain = domain;
            Service = service;
        }

        public string Domain { get; }

        public string Service { get; }
    }
}

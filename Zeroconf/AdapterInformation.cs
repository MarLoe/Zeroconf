using System;

namespace Zeroconf
{
    public readonly struct AdapterInformation : IEquatable<AdapterInformation>
    {
        public bool Equals(AdapterInformation other)
        {
            return string.Equals(Address, other.Address) && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            return obj is AdapterInformation information && Equals(information);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Address?.GetHashCode() ?? 0) * 397) ^ (Name?.GetHashCode() ?? 0);
            }
        }

        public static bool operator ==(AdapterInformation left, AdapterInformation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AdapterInformation left, AdapterInformation right)
        {
            return !left.Equals(right);
        }

        public AdapterInformation(string address, string name)
        {
            Address = address;
            Name = name;
        }

        public string Address { get; }

        public string Name { get; }

        public override string ToString()
        {
            return $"{Name}: {Address}";
        }
    }
}
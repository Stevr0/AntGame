using System;

namespace AntColony.Ants
{
    /// <summary>
    /// Stable identity for one ant. The stored text is deliberately independent
    /// of Unity instance IDs so a future save file can preserve it unchanged.
    /// </summary>
    public readonly struct AntId : IEquatable<AntId>
    {
        public AntId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An Ant ID cannot be empty.", nameof(value));
            }

            Value = value.Trim();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        /// <summary>Creates a unique ID suitable for a newly created ant record.</summary>
        public static AntId CreateUnique()
        {
            return new AntId(Guid.NewGuid().ToString("N"));
        }

        public bool Equals(AntId other)
        {
            return StringComparer.Ordinal.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is AntId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }

    /// <summary>Stable identity for the colony that owns an ant.</summary>
    public readonly struct ColonyId : IEquatable<ColonyId>
    {
        public ColonyId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A Colony ID cannot be empty.", nameof(value));
            }

            Value = value.Trim();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ColonyId other)
        {
            return StringComparer.Ordinal.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is ColonyId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }
}

using System;

namespace AntColony.World
{
    /// <summary>Stable simulation-region identity. It is not a camera identifier.</summary>
    public readonly struct RegionId : IEquatable<RegionId>, IComparable<RegionId>
    {
        public RegionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A region ID cannot be empty.", nameof(value));
            Value = value.Trim();
        }

        public string Value { get; }
        public bool Equals(RegionId other) =>
            StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is RegionId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 :
            StringComparer.Ordinal.GetHashCode(Value);
        public int CompareTo(RegionId other) =>
            StringComparer.Ordinal.Compare(Value, other.Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(RegionId left, RegionId right) => left.Equals(right);
        public static bool operator !=(RegionId left, RegionId right) => !left.Equals(right);
    }

    public readonly struct EntranceId : IEquatable<EntranceId>
    {
        public EntranceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("An entrance ID cannot be empty.", nameof(value));
            Value = value.Trim();
        }

        public string Value { get; }
        public bool Equals(EntranceId other) =>
            StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is EntranceId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 :
            StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct TransitionId : IEquatable<TransitionId>
    {
        public TransitionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A transition ID cannot be empty.", nameof(value));
            Value = value.Trim();
        }

        public string Value { get; }
        public bool Equals(TransitionId other) =>
            StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is TransitionId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 :
            StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>Colony-local integer cell. It never converts to a SurfaceCell.</summary>
    public readonly struct ColonyCell : IEquatable<ColonyCell>, IComparable<ColonyCell>
    {
        public ColonyCell(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public bool Equals(ColonyCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is ColonyCell other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public int CompareTo(ColonyCell other)
        {
            int xComparison = X.CompareTo(other.X);
            return xComparison != 0 ? xComparison : Y.CompareTo(other.Y);
        }
        public override string ToString() => $"({X}, {Y})";
    }

    /// <summary>Surface-local integer cell. It never converts to a ColonyCell.</summary>
    public readonly struct SurfaceCell : IEquatable<SurfaceCell>, IComparable<SurfaceCell>
    {
        public SurfaceCell(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public bool Equals(SurfaceCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is SurfaceCell other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public int CompareTo(SurfaceCell other)
        {
            int xComparison = X.CompareTo(other.X);
            return xComparison != 0 ? xComparison : Y.CompareTo(other.Y);
        }
        public override string ToString() => $"({X}, {Y})";
    }

    public enum RegionLocationKind { Colony, Surface }

    /// <summary>
    /// Identifies one cell without erasing whether its coordinates belong to the
    /// side-on Colony or top-down Surface. Callers must use the matching factory.
    /// </summary>
    public readonly struct RegionCellId : IEquatable<RegionCellId>,
        IComparable<RegionCellId>
    {
        private RegionCellId(RegionId regionId, RegionLocationKind kind,
            ColonyCell colonyCell, SurfaceCell surfaceCell)
        {
            if (string.IsNullOrWhiteSpace(regionId.Value))
                throw new ArgumentException("A cell requires a valid region ID.",
                    nameof(regionId));
            RegionId = regionId;
            Kind = kind;
            ColonyCell = colonyCell;
            SurfaceCell = surfaceCell;
        }

        public RegionId RegionId { get; }
        public RegionLocationKind Kind { get; }
        public ColonyCell ColonyCell { get; }
        public SurfaceCell SurfaceCell { get; }
        public static RegionCellId Colony(RegionId regionId, ColonyCell cell) =>
            new(regionId, RegionLocationKind.Colony, cell, default);
        public static RegionCellId Surface(RegionId regionId, SurfaceCell cell) =>
            new(regionId, RegionLocationKind.Surface, default, cell);
        public bool Equals(RegionCellId other) => RegionId == other.RegionId &&
            Kind == other.Kind && (Kind == RegionLocationKind.Colony
                ? ColonyCell.Equals(other.ColonyCell)
                : SurfaceCell.Equals(other.SurfaceCell));
        public override bool Equals(object obj) =>
            obj is RegionCellId other && Equals(other);
        public override int GetHashCode() => RegionId.GetHashCode() ^
            ((int)Kind * 397) ^ (Kind == RegionLocationKind.Colony
                ? ColonyCell.GetHashCode() : SurfaceCell.GetHashCode());
        public int CompareTo(RegionCellId other)
        {
            int regionComparison = RegionId.CompareTo(other.RegionId);
            if (regionComparison != 0) return regionComparison;
            int kindComparison = Kind.CompareTo(other.Kind);
            if (kindComparison != 0) return kindComparison;
            return Kind == RegionLocationKind.Colony
                ? ColonyCell.CompareTo(other.ColonyCell)
                : SurfaceCell.CompareTo(other.SurfaceCell);
        }
        public override string ToString() => Kind == RegionLocationKind.Colony
            ? $"{RegionId}:Colony{ColonyCell}"
            : $"{RegionId}:Surface{SurfaceCell}";
        public static bool operator ==(RegionCellId left, RegionCellId right) =>
            left.Equals(right);
        public static bool operator !=(RegionCellId left, RegionCellId right) =>
            !left.Equals(right);
    }

    /// <summary>A discriminated region destination; callers must inspect Kind.</summary>
    public readonly struct RegionDestination : IEquatable<RegionDestination>
    {
        private RegionDestination(RegionId regionId, RegionLocationKind kind,
            ColonyCell colonyCell, SurfaceCell surfaceCell)
        {
            RegionId = regionId;
            Kind = kind;
            ColonyCell = colonyCell;
            SurfaceCell = surfaceCell;
        }

        public RegionId RegionId { get; }
        public RegionLocationKind Kind { get; }
        public ColonyCell ColonyCell { get; }
        public SurfaceCell SurfaceCell { get; }
        public static RegionDestination Colony(RegionId id, ColonyCell cell) =>
            new(id, RegionLocationKind.Colony, cell, default);
        public static RegionDestination Surface(RegionId id, SurfaceCell cell) =>
            new(id, RegionLocationKind.Surface, default, cell);
        public bool Equals(RegionDestination other) => RegionId == other.RegionId &&
            Kind == other.Kind && ColonyCell.Equals(other.ColonyCell) &&
            SurfaceCell.Equals(other.SurfaceCell);
        public override bool Equals(object obj) =>
            obj is RegionDestination other && Equals(other);
        public override int GetHashCode() => RegionId.GetHashCode() ^
            ((int)Kind * 397) ^ ColonyCell.GetHashCode() ^ SurfaceCell.GetHashCode();
        public override string ToString() => Kind == RegionLocationKind.Colony
            ? $"{RegionId}:Colony{ColonyCell}"
            : $"{RegionId}:Surface{SurfaceCell}";
    }

    public static class AntRegionIds
    {
        public static readonly RegionId Colony = new("region.colony.founding-nest");
        public static readonly RegionId Surface = new("region.surface.founding-site");
    }
}

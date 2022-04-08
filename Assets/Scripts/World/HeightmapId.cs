using System;
public struct HeightmapId : IEquatable<HeightmapId>
{
    public readonly int X;
    public readonly int Z;
    public HeightmapId(int x, int z)
    {
        X = x;
        Z = z;
    }
    public static HeightmapId FromWorldPos(int x, int z)
    {
        //128, 2^7
        return new HeightmapId(x >> 7, z >> 7);
    }

    #region Equality members

    public bool Equals(HeightmapId other)
    {
        return X == other.X && Z == other.Z;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is HeightmapId other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = X;
            hashCode = (hashCode * 397) ^ Z;
            return hashCode;
        }
    }

    public static bool operator ==(HeightmapId left, HeightmapId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HeightmapId left, HeightmapId right)
    {
        return !left.Equals(right);
    }

    #endregion
}

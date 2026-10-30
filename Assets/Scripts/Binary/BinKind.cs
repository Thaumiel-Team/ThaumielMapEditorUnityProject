#if UNITY_EDITOR
namespace Assets.Scripts.Binary
{
    public enum BinKind : byte
    {
        Null = 0,
        Bool = 1,
        Byte = 2,
        SByte = 3,
        Int16 = 4,
        UInt16 = 5,
        Int32 = 6,
        UInt32 = 7,
        Int64 = 8,
        UInt64 = 9,
        Float = 10,
        Double = 11,
        Decimal = 12,
        Char = 13,
        String = 14,
        DateTime = 15,
        TimeSpan = 16,
        Guid = 17,
        ByteArray = 18,
        DateTimeOffset = 19,
        Uri = 20,
        List = 21,
        Dictionary = 22,
        Object = 23,
        RuntimeTyped = 24,
    }
}
#endif
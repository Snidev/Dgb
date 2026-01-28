using System.Numerics;

namespace Dgb.Helpers;

public static class BinaryHelper
{
    public static bool GetBitFromInteger<T>(T value, int bit) where T : IBinaryInteger<T>, IBitwiseOperators<T, T, T>
    {
        T mask = T.One << bit;
        return (value & mask) == mask;
    }

    public static T SetBitForInteger<T>(T value, int bit, bool bitValue) where T : IBinaryInteger<T>, IBitwiseOperators<T, T, T>
    {
        T mask = T.One << bit;
        return bitValue ? (value | mask) : (value & ~mask);
    }
}
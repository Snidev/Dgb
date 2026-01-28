namespace Dgb.ByteDecoder;

public readonly struct VariableInfo(char name, int start, int length, byte mask)
{
    public readonly char Name = name;
    public readonly int Start = start;
    public readonly int Length = length;
    public readonly byte Mask = mask;

    public int Extract(byte value) => (value & Mask) >> (Start - Length + 1);
}
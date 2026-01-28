namespace Dgb.Core.Bus;

public interface IBusConnection
{
    public byte ReadByte(ushort addr);
    public void WriteByte(ushort addr, byte value);
}
using Dgb.Helpers;

namespace Dgb.Core.Cpu;

public static class BinaryArithmetic
{
    public static Registers Add8(Registers r, byte value, bool carry = false)
    {
        byte c = (byte)(r.FlagC && carry ? 1 : 0);
        int temp = r.A + c + value;

        r.FlagZ = (temp & 0xFF) == 0;
        r.FlagN = false;
        r.FlagH = (((r.A & 0x0F) + c + (value & 0x0F)) & 0x10) != 0;
        r.FlagC = BinaryHelper.GetBitFromInteger(temp, 8);

        r.A = (byte)temp;

        return r;
    }

    public static Registers Sub8(Registers r, byte value, bool carry = false)
    {
        byte c = (byte)(r.FlagC && carry ? 1 : 0);
        uint temp = (uint)(r.A - (value + c));

        r.FlagZ = (temp & 0xFF) == 0;
        r.FlagN = true;
        r.FlagH = (((r.A & 0x0F) - ((value & 0x0F) + c)) & 0x10) != 0;
        r.FlagC = temp >= 0x100;

        r.A = (byte)temp;

        return r;
    }

    public static Registers Compare(Registers r, byte value)
    {
        Registers subResult = Sub8(r, value);
        r.F = subResult.F;
        return r;
    }

    public static (Registers, byte) Increment(Registers r, byte value)
    {
        Registers dummy = r;
        dummy.A = value;
        dummy = Add8(dummy, 1);

        dummy.FlagC = r.FlagC;
        r.F = dummy.F;

        return (r, dummy.A);
    }

    public static Registers Increment(Registers r, R8 target)
    {
        (r, byte res) = Increment(r, r[target]);
        r[target] = res;
        return r;
    }

    public static (Registers, byte) Decrement(Registers r, byte value)
    {
        Registers dummy = r;
        dummy.A = value;
        dummy = Sub8(dummy, 1);

        dummy.FlagC = r.FlagC;
        r.F = dummy.F;

        return (r, dummy.A);
    }

    public static Registers Decrement(Registers r, R8 target)
    {
        (r, byte res) = Decrement(r, r[target]);
        r[target] = res;
        return r;
    }

    public static Registers And(Registers r, byte value)
    {
        r.A &= value;
        r.F = (byte)(r.A == 0 ? 0b1010_0000 : 0b0010_0000);
        return r;
    }

    public static Registers Cpl(Registers r)
    {
        r.A = (byte)~r.A;
        r.FlagN = true;
        r.FlagH = true;
        return r;
    }

    public static Registers Or(Registers r, byte value)
    {
        r.A |= value;
        r.F = (byte)(r.A == 0 ? 0x80 : 0);
        return r;
    }

    public static Registers Xor(Registers r, byte value)
    {
        r.A ^= value;
        r.F = (byte)(r.A == 0 ? 0x80 : 0);
        return r;
    }

    public static Registers Test(Registers r, byte value, int bit)
    {
        r.FlagZ = !BinaryHelper.GetBitFromInteger(value, bit);
        r.FlagN = false;
        r.FlagH = true;
        return r;
    }

    public static (Registers, byte) Rl(Registers r, byte value, bool setZ = true)
    {
        int temp = (value << 1) | (r.FlagC ? 1 : 0);
        value = (byte)temp;

        r.FlagZ = value == 0 && setZ;
        r.FlagN = false;
        r.FlagH = false;
        r.FlagC = temp >= 0x100;

        return (r, value);
    }

    public static Registers Rl(Registers r, R8 target, bool setZ = true)
    {
        (r, byte res) = Rl(r, r[target], setZ);
        r[target] = res;
        return r;
    }

    public static (Registers, byte) Rlc(Registers r, byte value, bool setZ = true)
    {
        r.FlagC = value >= 0x80;

        value = (byte)((value >> 7) | (value << 1));

        r.FlagZ = value == 0 && setZ;
        r.FlagN = false;
        r.FlagH = false;

        return (r, value);
    }

    public static Registers Rlc(Registers r, R8 target, bool setZ = true)
    {
        (r, byte res) = Rlc(r, r[target], setZ);
        r[target] = res;
        return r;
    }

    public static (Registers, byte) Rr(Registers r, byte value, bool setZ = true)
    {
        bool newC = (value & 0b1) == 0b1;

        value = (byte)((value >> 1) | (r.FlagC ? 0x80 : 0));

        r.FlagZ = value == 0 && setZ;
        r.FlagN = false;
        r.FlagH = false;
        r.FlagC = newC;
        
        return (r, value);
    }

    public static Registers Rr(Registers r, R8 target, bool setZ = true)
    {
        (r, byte res) = Rr(r, r[target], setZ);
        r[target] = res;
        return r;
    }
    
    public static (Registers, byte) Rrc(Registers r, byte value, bool setZ = true)
    {
        r.FlagC = (value & 0b1) == 0b1;

        value = (byte)((value >> 1) | (value << 7));

        r.FlagZ = value == 0 && setZ;
        r.FlagN = false;
        r.FlagH = false;

        return (r, value);
    }

    public static Registers Rrc(Registers r, R8 target, bool setZ = true)
    {
        (r, byte res) = Rrc(r, r[target], setZ);
        r[target] = res;
        return r;
    }

    public static (Registers, byte) Sla(Registers r, byte value)
    {
        r.FlagC = BinaryHelper.GetBitFromInteger(value, 7);

        value <<= 1;

        r.FlagZ = value == 0;
        r.FlagN = false;
        r.FlagH = false;

        return (r, value);
    }

    public static Registers Sla(Registers r, R8 target)
    {
        (r, byte res) = Sla(r, r[target]);
        r[target] = res;
        return r;
    }

    public static (Registers, byte) Sra(Registers r, byte value)
    {
        r.FlagC = BinaryHelper.GetBitFromInteger(value, 0);
        
        value = (byte)((value & 0x80) | (value >> 1));

        r.FlagZ = value == 0;
        r.FlagN = false;
        r.FlagH = false;

        return (r, value);
    }

    public static Registers Sra(Registers r, R8 target)
    {
        (r, byte res) = Sra(r, r[target]);
        r[target] = res;
        return r;
    }

    public static (Registers, byte) Srl(Registers r, byte value)
    {
        r.FlagC = BinaryHelper.GetBitFromInteger(value, 0);

        value >>= 1;

        r.FlagZ = value == 0;
        r.FlagN = false;
        r.FlagH = false;

        return (r, value);
    }

    public static Registers Srl(Registers r, R8 target)
    {
        (r, byte res) = Srl(r, r[target]);
        r[target] = res;
        return r;
    }

    public static Registers Daa(Registers r)
    {
        byte correction = 0;
        bool setFlagC = false;

        if (r.FlagH || (!r.FlagN && (r.A & 0x0F) > 9))
            correction |= 0x06;

        if (r.FlagC || (!r.FlagN && r.A > 0x99))
        {
            correction |= 0x60;
            setFlagC = true;
        }

        r.A += (byte)(r.FlagN ? -correction : correction);

        r.FlagZ = r.A == 0;
        r.FlagH = false;
        r.FlagC = setFlagC;

        return r;
    }
    
    public static (Registers, byte) Swap(Registers r, byte value)
    {
        value = (byte)((value << 4) | (value >> 4));
        r.F = (byte)(value == 0 ? 0b1000_0000 : 0);
        return (r, value);
    }

    public static Registers Swap(Registers r, R8 target)
    {
        (r, byte res) = Swap(r, r[target]);
        r[target] = res;
        return r;
    }

    public static Registers Add16(Registers r, R16 target)
    {
        int temp = r.HL + r[target];

        r.FlagN = false;
        r.FlagH = BinaryHelper.GetBitFromInteger((r.HL & 0xFFF) + (r[target] & 0xFFF), 12);
        r.FlagC = BinaryHelper.GetBitFromInteger(temp, 16);

        r.HL = (ushort)temp;

        return r;
    }
}
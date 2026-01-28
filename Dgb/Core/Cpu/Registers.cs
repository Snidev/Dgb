using System.ComponentModel;
using Dgb.Helpers;

namespace Dgb.Core.Cpu;

public struct Registers
{
    public const int BitZ = 7;
    public const int BitN = 6;
    public const int BitH = 5;
    public const int BitC = 4;
    
    private byte _f;

    public byte A;
    public byte B;
    public byte C;
    public byte D;
    public byte E;

    public byte F
    {
        get => _f;
        set => _f = (byte)(value & 0xF0);
    }

    public byte H;
    public byte L;

    public ushort AF 
    {
        get => (ushort)((A << 8) | F);
        set
        {
            A = (byte)(value >> 8);
            F = (byte)value;
        }
    }
    
    public ushort BC 
    {
        get => (ushort)((B << 8) | C);
        set
        {
            B = (byte)(value >> 8);
            C = (byte)value;
        }
    }
    
    public ushort DE 
    {
        get => (ushort)((D << 8) | E);
        set
        {
            D = (byte)(value >> 8);
            E = (byte)value;
        }
    }
    
    public ushort HL 
    {
        get => (ushort)((H << 8) | L);
        set
        {
            H = (byte)(value >> 8);
            L = (byte)value;
        }
    }

    public ushort Sp { get; set; }
    public ushort Pc { get; set; }

    public bool FlagZ
    {
        get => BinaryHelper.GetBitFromInteger(_f, BitZ);
        set => _f = BinaryHelper.SetBitForInteger(_f, BitZ, value);
    }
    
    public bool FlagN
    {
        get => BinaryHelper.GetBitFromInteger(_f, BitN);
        set => _f = BinaryHelper.SetBitForInteger(_f, BitN, value);
    }
    
    public bool FlagH
    {
        get => BinaryHelper.GetBitFromInteger(_f, BitH);
        set => _f = BinaryHelper.SetBitForInteger(_f, BitH, value);
    }
    
    public bool FlagC
    {
        get => BinaryHelper.GetBitFromInteger(_f, BitC);
        set => _f = BinaryHelper.SetBitForInteger(_f, BitC, value);
    }

    public byte this[R8 i]
    {
        get => i switch
        {
            R8.A => A,
            R8.B => B,
            R8.C => C,
            R8.D => D,
            R8.E => E,
            R8.F => F,
            R8.H => H,
            R8.L => L,
            _ => throw new InvalidEnumArgumentException()
        };
        set
        {
            switch (i)
            {
                case R8.A:
                    A = value;
                    break;
                case R8.B:
                    B = value;
                    break;
                case R8.C:
                    C = value;
                    break;
                case R8.D:
                    D = value;
                    break;
                case R8.E:
                    E = value;
                    break;
                case R8.F:
                    F = value;
                    break;
                case R8.H:
                    H = value;
                    break;
                case R8.L:
                    L = value;
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }

    public ushort this[R16 i]
    {
        get => i switch
        {
            R16.AF => AF,
            R16.BC => BC,
            R16.DE => DE,
            R16.HL => HL,
            R16.Pc => Pc,
            R16.Sp => Sp,
            _ => throw new InvalidEnumArgumentException()
        };
        set
        {
            switch (i)
            {
                case R16.AF:
                    AF = value;
                    break;
                case R16.BC:
                    BC = value;
                    break;
                case R16.DE:
                    DE = value;
                    break;
                case R16.HL:
                    HL = value;
                    break;
                case R16.Pc:
                    Pc = value;
                    break;
                case R16.Sp:
                    Sp = value;
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }
}
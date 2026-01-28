using System.ComponentModel;

namespace Dgb.Core.Cpu;

public static class InstructionOperandLookup
{
    public static R8 GetR8(int i) => i switch
    {
        0 => R8.B,
        1 => R8.C,
        2 => R8.D,
        3 => R8.E,
        4 => R8.H,
        5 => R8.L,
        7 => R8.A,
        _ => throw new InvalidEnumArgumentException()
    };

    public static R16 GetR16(int i) => i switch
    {
        0 => R16.BC,
        1 => R16.DE,
        2 => R16.HL,
        3 => R16.Sp,
        _ => throw new InvalidEnumArgumentException()
    };

    public static R16 GetR16Stack(int i) => i switch
    {
        0 => R16.BC,
        1 => R16.DE,
        2 => R16.HL,
        3 => R16.AF,
        _ => throw new InvalidEnumArgumentException()
    };

    public static R16 GetR16Mem(int i) => i switch
    {
        0 => R16.BC,
        1 => R16.DE,
        _ => throw new InvalidEnumArgumentException()
    };

    public static bool EvalConditional(Registers reg, int cc) => cc switch
    {
        0 => !reg.FlagZ,
        1 => reg.FlagZ,
        2 => !reg.FlagC,
        3 => reg.FlagC,
        _ => throw new InvalidEnumArgumentException()
    };
}
using System.ComponentModel;

namespace dngb.Core.Cpu;

public class Processor
{
    public Registers Registers;

    private uint _executionTime = 0;
    
    public void Execute(byte opcode)
    {
        switch (opcode)
        {
            // $00 nop
            case 0x00:
                break;
            
            // ld r16, u16
            case 0x01: case 0x11: case 0x21: case 0x31:
            {
                R16 dst = GetReg16((opcode & 0x30) >> 4);
                Registers[dst] = Immediate16();
            }   
                break;
            
            // ld [r16], a
            case 0x02: case 0x12:
                WriteByte(Registers[GetReg16((opcode & 0x30) >> 4)], Registers.A);
                break;
            case 0x22:
                WriteByte(Registers.HL++, Registers.A);
                break;
            case 0x32:
                WriteByte(Registers.HL--, Registers.A);
                break;
            
            // inc r16
            case 0x03: case 0x13: case 0x23: case 0x33:
                Registers[GetReg16((opcode & 0x30) >> 4)]++;
                break;
            
            // Increment operations
            case 0x04: case 0x0C: case 0x14: case 0x1C: case 0x24: case 0x2C: case 0x3C:
                Registers = BinaryArithmetic.Increment(Registers, GetReg8FromTableA((opcode & 0x38) >> 3));
                break;

            case 0x34:
            {
                (Registers, byte res) = BinaryArithmetic.Increment(Registers, ReadValue(Registers.HL));
                WriteByte(Registers.HL, res);
            }
                break;
            
            // Decrement operations
            case 0x05: case 0x0D: case 0x15: case 0x1D: case 0x25: case 0x2D: case 0x3D:
                Registers = BinaryArithmetic.Decrement(Registers, GetReg8FromTableA((opcode & 0x38) >> 3));
                break;
            
            case 0x35:
            {
                (Registers, byte res) = BinaryArithmetic.Decrement(Registers, ReadValue(Registers.HL));
                WriteByte(Registers.HL, res);
            }
                break;
            
            // Immediate loads
            case 0x06: case 0x0E: case 0x16: case 0x1E: case 0x26: case 0x2E: case 0x3E:
            {
                R8 dst = GetReg8FromTableA((opcode & 0x38) >> 3);
                Registers[dst] = Immediate();
            }
                break;
            
            case 0x36: // special case - ld [hl], u8
                WriteByte(Registers.HL, Immediate());
                break;
            
            // rlca
            case 0x07:
                Registers = BinaryArithmetic.Rlc(Registers, R8.A, false);
                break;
            
            // store stack pointer
            case 0x08:
                WriteShort(Immediate16(), Registers.Sp);
                break; 
            
            // 16 bit adds
            case 0x09: case 0x19: case 0x29: case 0x39:
            {
                R16 src = GetReg16((opcode & 0x30) >> 4);
                Registers = BinaryArithmetic.Add16(Registers, src);
            }   
                break;
                
            // read into A from register
            case 0x0A: case 0x1A:
            {
                R16 src = GetReg16((opcode & 0x30) >> 4);
                Registers.A = ReadValue(Registers[src]);
            }   
                break;
            
            case 0x2A:
                Registers.A = ReadValue(Registers.HL++);
                break;
            
            case 0x3A:
                Registers.A = ReadValue(Registers.HL--);
                break;
            
            // Decrement 16 bit
            case 0x0B: case 0x1B: case 0x2B: case 0x3B:
            {
                R16 src = GetReg16((opcode & 0x30) >> 4);
                Registers[src]--;
            }   
                break;
            
            // rrca
            case 0x0F:
                Registers = BinaryArithmetic.Rrc(Registers, R8.A, false);
                break;
            
            // stop
            case 0x10:
                throw new NotImplementedException();
                
            // rla
            case 0x17:
                Registers = BinaryArithmetic.Rl(Registers, R8.A, false);
                break;
            
            // jr i8
            case 0x18:
                Registers.Pc = (ushort)(Registers.Pc + (sbyte)Immediate());
                break;
            
            // rra
            case 0x1F:
                Registers = BinaryArithmetic.Rr(Registers, R8.A, false);
                break;
            
            // jr nz, i8 todo: Cycle changes
            case 0x20:
                if (!Registers.FlagZ)
                {
                    Registers.Pc = (ushort)(Registers.Pc + (sbyte)Immediate());
                    _executionTime += 4;
                }

                break;
            
            // daa
            case 0x27:
                throw new NotImplementedException();
            
            // jr z, i8 todo: Cycle changes
            case 0x28:
                if (Registers.FlagZ)
                {
                    Registers.Pc = (ushort)(Registers.Pc + (sbyte)Immediate());
                    _executionTime += 4;
                }
                break;
            
            // cpl
            case 0x2F:
                Registers = BinaryArithmetic.Cpl(Registers);
                break;
            
            // jr nc, i8 todo: Cycle changes
            case 0x30:
                if (!Registers.FlagC)
                {
                    Registers.Pc = (ushort)(Registers.Pc + (sbyte)Immediate());
                    _executionTime += 4;
                }
                break;
            
            // scf
            case 0x37:
                Registers.F = (byte)((Registers.F & 0x80) | 0x10);
                break;
            
            // jr c, i8 todo: Cycle changes
            case 0x38:
                if (Registers.FlagC)
                {
                    Registers.Pc = (ushort)(Registers.Pc + (sbyte)Immediate());
                    _executionTime += 4;
                }
                break;
            
            // ccf
            case 0x3F:
                Registers.F = (byte)((Registers.F & 0x80) | (byte)(Registers.FlagC ? 0 : 0x10));
                break;
            
            // LD opcodes ($40 - $7F)
            // Special cases: load from [HL]
            case 0x46: case 0x4E: case 0x56: case 0x5E: case 0x66: case 0x6E:
            {
                R8 dst = GetReg8FromTableA((opcode & 0x38) >> 3);
                Registers[dst] = ReadValue(Registers.HL);
            }
                break;
            
            case >= 0x40 and <= 0x6F:
            {
                R8 src = GetReg8FromTableA(opcode & 0x07);
                R8 dst = GetReg8FromTableA((opcode & 0x38) >> 3);
                Registers[dst] = Registers[src];
            }
                break;
            
            // Special case: Halt
            case 0x76:
                throw new NotImplementedException();
                break;
                
            // Special cases: load into [HL]
            case >= 0x70 and <= 0x7F:
            {
                R8 src = GetReg8FromTableA(opcode & 0x07);
                WriteByte(Registers.HL, Registers[src]);
            }
                break;
            
            // ADD opcodes ($80 - $87)
            case 0x86: // Special case Add (HL)
                Registers = BinaryArithmetic.Add8(Registers, ReadValue(Registers.HL));
                break;
            case >= 0x80 and <= 0x87:
                Registers = BinaryArithmetic.Add8(Registers, Registers[GetReg8FromTableA(opcode & 0x0F)]);
                break;
            
            // ADC opcodes ($88 - $8F)
            case 0x8E: // Special case Adc (HL)
                Registers = BinaryArithmetic.Add8(Registers, ReadValue(Registers.HL), true);
                break;
            case >= 0x88 and <= 0x8F:
                Registers = BinaryArithmetic.Add8(Registers, Registers[GetReg8FromTableA((opcode & 0x0F) - 8)], true);
                break;
            
            // Sub opcodes ($90 - $97)
            case 0x96: // Special case Sub (HL)
                Registers = BinaryArithmetic.Sub8(Registers, ReadValue(Registers.HL));
                break;
            case >= 0x90 and <= 0x97:
                Registers = BinaryArithmetic.Sub8(Registers, Registers[GetReg8FromTableA(opcode & 0x0F)]);
                break;
            
            // Sbc opcodes ($98 - $9F)
            case 0x9E: // Special case Sbc (HL)
                Registers = BinaryArithmetic.Sub8(Registers, ReadValue(Registers.HL), true);
                break;
            case >= 0x98 and <= 0x9F:
                Registers = BinaryArithmetic.Sub8(Registers, Registers[GetReg8FromTableA((opcode & 0x0F) - 8)], true);
                break;
            
            // AND opcodes ($A0 - $A7)
            case 0xA6: // Special case And (HL)
                Registers = BinaryArithmetic.And(Registers, ReadValue(Registers.HL));
                break;
            case >= 0xA0 and <= 0xA7:
                Registers = BinaryArithmetic.And(Registers, Registers[GetReg8FromTableA(opcode & 0x0F)]);
                break;
            
            // Xor opcodes ($A8 - $AF)
            case 0xAE: // Special case Xor (HL)
                Registers = BinaryArithmetic.Xor(Registers, ReadValue(Registers.HL));
                break;
            case >= 0xA8 and <= 0xAF:
                Registers = BinaryArithmetic.Xor(Registers, Registers[GetReg8FromTableA((opcode & 0x0F) - 8)]);
                break;
            
            // OR opcodes ($B0 - $B7)
            case 0xB6: // Special case Or (HL)
                Registers = BinaryArithmetic.Or(Registers, ReadValue(Registers.HL));
                break;
            case >= 0xB0 and <= 0xB7:
                Registers = BinaryArithmetic.Or(Registers, Registers[GetReg8FromTableA(opcode & 0x0F)]);
                break;
            
            // Xor opcodes ($B8 - $BF)
            case 0xBE: // Special case Xor (HL)
                Registers = BinaryArithmetic.Compare(Registers, ReadValue(Registers.HL));
                break;
            case >= 0xB8 and <= 0xBF:
                Registers = BinaryArithmetic.Compare(Registers, Registers[GetReg8FromTableA((opcode & 0x0F) - 8)]);
                break;
            
            // ret nz 
            case 0xC0:
                if (!Registers.FlagZ)
                {
                    _executionTime += 12;
                    Registers.Pc = Pop();
                }
                break;
            
            // Pop operations
            case 0xC1: case 0xD1: case 0xE1: case 0xF1:
                Registers[GetStackTarget((opcode & 0xC0) >> 6)] = Pop();
                break;
                    
            // jp nz, u16
            case 0xC2:
                if (!Registers.FlagZ)
                {
                    _executionTime += 4;
                    Registers.Pc = Immediate16();
                }
                break;
            
            // jp u16
            case 0xC3:
                Registers.Pc = Immediate16();
                break;
            
            // call nz, u16
            case 0xC4:
                if (!Registers.FlagZ)
                {
                    _executionTime += 12;
                    Push(Registers.Pc);
                    Registers.Pc = Immediate16();
                }
                break;
            
            // push operations
            case 0xC5: case 0xD5: case 0xE5: case 0xF5:
                Push(Registers[GetStackTarget((opcode & 0xC0) >> 6)]);
                break;
            
            // add a, u8
            case 0xC6: 
                Registers = BinaryArithmetic.Add8(Registers, Immediate());
                break;
            
            // reset vectors
            case 0xC7: case 0xD7: case 0xE7: case 0xF7:
                Push(Registers.Sp);
                Registers.Pc = GetVector(2); //todo wrong as fuck
                break;
            
            default:
                Console.Write($"{opcode:X2} ");
                break;
        }

        void Push(ushort value)
        {
            WriteByte(--Registers.Sp, (byte)(value >> 8));
            WriteByte(--Registers.Sp, (byte)value);
        }

        ushort Pop()
        {
            byte low = ReadValue(Registers.Sp++);
            return (ushort)((ReadValue(Registers.Sp++) << 8) | low);
        }
        
        ushort Immediate16()
        {
            return 0;
        }
        
        byte Immediate()
        {
            return 0;
        }

        byte ReadValue(ushort addr)
        {
            return 0;
        }

        void WriteByte(ushort addr, byte value)
        {
            
        }

        void WriteShort(ushort addr, ushort value) {}
        
        R8 GetReg8FromTableA(int val) => val switch
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

        R16 GetReg16(int val) => val switch
        {
            0 => R16.BC,
            1 => R16.DE,
            2 => R16.HL,
            3 => R16.Sp,
            _ => throw new InvalidEnumArgumentException()
        };

        R16 GetStackTarget(int val) => val switch
        {
            0 => R16.BC,
            1 => R16.DE,
            2 => R16.HL,
            3 => R16.AF,
            _ => throw new InvalidEnumArgumentException()
        };

        ushort GetVector(int val) => (ushort)(8 * val);
    }
}
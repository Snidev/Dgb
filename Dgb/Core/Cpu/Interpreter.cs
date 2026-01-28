using System.Diagnostics;
using Dgb.ByteDecoder;
using Dgb.Core.Bus;
using Dgb.Helpers;

namespace Dgb.Core.Cpu;

public class Interpreter
{
    public delegate int Instruction(Processor processor, IBusConnection bus, BytePattern pattern, byte opcode);

    private readonly Dictionary<string, Instruction>[] _instructions = [new(), new()];
    private readonly PatternTree[] _patternTree = [new(), new()];

    private void AddInstruction(string pattern, Instruction instruction, int table = 0)
    {
        _patternTree[table].AddPattern(pattern);
        _instructions[table].Add(pattern, instruction);
    }

    public int Execute(Processor processor, IBusConnection bus, byte opcode, out BytePattern usedPattern, int table = 0)
    {
        if (!_patternTree[table].Test(opcode, out BytePattern? pattern))
            throw new InstructionCoverageException();

        usedPattern = pattern;
        return _instructions[table][pattern.Pattern].Invoke(processor, bus, pattern, opcode);
    }

    public int Execute(Processor processor, IBusConnection bus, byte opcode) => Execute(processor, bus, opcode, out _);

    public static Interpreter CreateDmgInterpreter()
    {
        Interpreter interpreter = new Interpreter();
        
        // Primary opcode table - Block 0//
        interpreter.AddInstruction("00xxxxxx", Group);
        // nop
        interpreter.AddInstruction("00000000", (_, _, _, _) => 0);
        // ld r16,u16
        interpreter.AddInstruction("00dd0001", (processor, bus, pattern, opcode) =>
        {
            R16 dst = InstructionOperandLookup.GetR16(pattern.Variables['d'].Extract(opcode));
            byte low = bus.ReadByte(processor.Registers.Pc++);
            byte high = bus.ReadByte(processor.Registers.Pc++);
            
            processor.Registers[dst] = (ushort)((high << 8) | low);
            
            return 0;
        });
        
        // ld [r16], a
        interpreter.AddInstruction("000d0010", (processor, bus, pattern, opcode) =>
        {
            R16 dst = InstructionOperandLookup.GetR16Mem(pattern.Variables['d'].Extract(opcode));
            bus.WriteByte(processor.Registers[dst], processor.Registers.A);
            return 0;
        });
        interpreter.AddInstruction("00100010", (processor, bus, _, _) =>
        {
            bus.WriteByte(processor.Registers.HL++, processor.Registers.A);
            return 0;
        });
        interpreter.AddInstruction("00110010", (processor, bus, _, _) =>
        {
            bus.WriteByte(processor.Registers.HL--, processor.Registers.A);
            return 0;
        });
        
        // ld a,[r16]
        interpreter.AddInstruction("000s1010", (processor, bus, pattern, opcode) =>
        {
            R16 src = InstructionOperandLookup.GetR16Mem(pattern.Variables['s'].Extract(opcode));
            processor.Registers.A = bus.ReadByte(processor.Registers[src]);
            return 0;
        });
        interpreter.AddInstruction("00101010", (processor, bus, _, _) =>
        {
            processor.Registers.A = bus.ReadByte(processor.Registers.HL++);
            return 0;
        });
        interpreter.AddInstruction("00111010", (processor, bus, _, _) =>
        {
            processor.Registers.A = bus.ReadByte(processor.Registers.HL--);
            return 0;
        });
        
        // ld [u16],sp
        interpreter.AddInstruction("00001000", (processor, bus, _, _) =>
        {
            byte low = bus.ReadByte(processor.Registers.Pc++);
            byte high = bus.ReadByte(processor.Registers.Pc++);
            ushort dst = (ushort)(high << 8 | low);
            bus.WriteByte(dst++, (byte)processor.Registers.Sp);
            bus.WriteByte(dst, (byte)(processor.Registers.Sp >> 8));
            return 0;
        });
        
        // inc r16
        interpreter.AddInstruction("00oo0011", (processor, _, pattern, opcode) =>
        {
            R16 reg = InstructionOperandLookup.GetR16(pattern.Variables['o'].Extract(opcode));
            processor.Registers[reg]++;            
            
            return 0;
        });
        
        // dec r16
        interpreter.AddInstruction("00oo1011", (processor, _, pattern, opcode) =>
        {
            R16 reg = InstructionOperandLookup.GetR16(pattern.Variables['o'].Extract(opcode));
            processor.Registers[reg]--;
            
            return 0;
        });
        
        // add hl,r16
        interpreter.AddInstruction("00oo1001", (processor, _, pattern, opcode) =>
        {
            R16 reg = InstructionOperandLookup.GetR16(pattern.Variables['o'].Extract(opcode));
            processor.Registers = BinaryArithmetic.Add16(processor.Registers, reg);

            return 0;
        });
        
        // inc r8
        interpreter.AddInstruction("00ooo100", (processor, _, pattern, opcode) =>
        {
            R8 r8 = InstructionOperandLookup.GetR8(pattern.Variables['o'].Extract(opcode));
            processor.Registers = BinaryArithmetic.Increment(processor.Registers, r8);

            return 0;
        });
        interpreter.AddInstruction("00110100", (processor, bus, _, _) =>
        {
            byte val = bus.ReadByte(processor.Registers.HL);
            (processor.Registers, val) = BinaryArithmetic.Increment(processor.Registers, val);
            bus.WriteByte(processor.Registers.HL, val);
            
            return 0;
        });
        
        // dec r8
        interpreter.AddInstruction("00ooo101", (processor, _, pattern, opcode) =>
        {
            R8 r8 = InstructionOperandLookup.GetR8(pattern.Variables['o'].Extract(opcode));
            processor.Registers = BinaryArithmetic.Decrement(processor.Registers, r8);

            return 0;
        });
        interpreter.AddInstruction("00110101", (processor, bus, _, _) =>
        {
            byte val = bus.ReadByte(processor.Registers.HL);
            (processor.Registers, val) = BinaryArithmetic.Decrement(processor.Registers, val);
            bus.WriteByte(processor.Registers.HL, val);
            
            return 0;
        });
        
        // ld r8,u8
        interpreter.AddInstruction("00ddd110", (processor, bus, pattern, opcode) =>
        {
            R8 r8 = InstructionOperandLookup.GetR8(pattern.Variables['d'].Extract(opcode));
            processor.Registers[r8] = bus.ReadByte(processor.Registers.Pc++);

            return 0;
        });
        interpreter.AddInstruction("00110110", (processor, bus, _, _) =>
        {
            bus.WriteByte(processor.Registers.HL, bus.ReadByte(processor.Registers.Pc++));
            return 0;
        });
        
        // rlca
        interpreter.AddInstruction("00000111", (processor, _, _, _) =>
        {
            processor.Registers = BinaryArithmetic.Rlc(processor.Registers, R8.A, false);
            return 0;
        });
        // rrca
        interpreter.AddInstruction("00001111", (processor, _, _, _) =>
        {
            processor.Registers = BinaryArithmetic.Rrc(processor.Registers, R8.A, false);
            return 0;
        });
        // rla
        interpreter.AddInstruction("00010111", (processor, _, _, _) =>
        {
            processor.Registers = BinaryArithmetic.Rl(processor.Registers, R8.A, false);
            return 0;
        });
        // rra
        interpreter.AddInstruction("00011111", (processor, _, _, _) =>
        {
            processor.Registers = BinaryArithmetic.Rr(processor.Registers, R8.A, false);
            return 0;
        });
        
        // daa 
        interpreter.AddInstruction("00100111", (processor, _, _, _) =>
        {
            processor.Registers = BinaryArithmetic.Daa(processor.Registers);
            return 0;
        });
        
        // cpl
        interpreter.AddInstruction("00101111", (processor, _, _, _) =>
        {
            processor.Registers = BinaryArithmetic.Cpl(processor.Registers);
            return 0;
        });
        
        // scf
        interpreter.AddInstruction("00110111", (processor, _, _, _) =>
        {
            processor.Registers.FlagN = false;
            processor.Registers.FlagH = false;
            processor.Registers.FlagC = true;
            return 0;
        });
        
        // ccf 
        interpreter.AddInstruction("00111111", (processor, _, _, _) =>
        {
            processor.Registers.FlagN = false;
            processor.Registers.FlagH = false;
            processor.Registers.FlagC = !processor.Registers.FlagC;
            return 0;
        });
        
        // jr i8
        interpreter.AddInstruction("00011000", (processor, bus, _, _) =>
        {
            sbyte offset = (sbyte)bus.ReadByte(processor.Registers.Pc++);
            processor.Registers.Pc = (ushort)(processor.Registers.Pc + offset);
            
            return 0;
        });
        
        // jr cc,i8 
        interpreter.AddInstruction("001cc000", (processor, bus, pattern, opcode) =>
        {
            int cc = pattern.Variables['c'].Extract(opcode);
            sbyte operand = (sbyte)bus.ReadByte(processor.Registers.Pc++);
            
            if (!InstructionOperandLookup.EvalConditional(processor.Registers, cc)) 
                return 0;
            
            processor.Registers.Pc = (ushort)(processor.Registers.Pc + operand);
            return 4;

        });
        
        // stop
        interpreter.AddInstruction("00010000", (_, _, _, _) 
            => throw new NotImplementedException());
        
        // Block 1 - ld r8,r8
        interpreter.AddInstruction("01dddsss", (processor, _, pattern, opcode) =>
        {
            R8 dst = InstructionOperandLookup.GetR8(pattern.Variables['d'].Extract(opcode));
            R8 src = InstructionOperandLookup.GetR8(pattern.Variables['s'].Extract(opcode));
            processor.Registers[dst] = processor.Registers[src];
            
            return 0;
        });
        interpreter.AddInstruction("01110sss", (processor, bus, pattern, opcode) =>
        {
            R8 src = InstructionOperandLookup.GetR8(pattern.Variables['s'].Extract(opcode));
            bus.WriteByte(processor.Registers.HL, processor.Registers[src]);

            return 0;
        });
        interpreter.AddInstruction("01ddd110", (processor, bus, pattern, opcode) =>
        {
            R8 dst = InstructionOperandLookup.GetR8(pattern.Variables['d'].Extract(opcode));
            processor.Registers[dst] = bus.ReadByte(processor.Registers.HL);
            
            return 0;
        });
        // halt
        interpreter.AddInstruction("01110110",  (_, _, _, _) 
            => throw new NotImplementedException());
        
        // alu a,r8
        interpreter.AddInstruction("10oooxxx", (processor, bus, pattern, opcode) =>
        {
            byte operand = pattern.Variables['x'].Extract(opcode) switch
            {
                0 => processor.Registers.B,
                1 => processor.Registers.C,
                2 => processor.Registers.D,
                3 => processor.Registers.E,
                4 => processor.Registers.H,
                5 => processor.Registers.L,
                6 => bus.ReadByte(processor.Registers.HL),
                7 => processor.Registers.A,
                _ => throw new UnreachableException(),
            };

            processor.Registers = pattern.Variables['o'].Extract(opcode) switch
            {
                0 => BinaryArithmetic.Add8(processor.Registers, operand),
                1 => BinaryArithmetic.Add8(processor.Registers, operand, true),
                2 => BinaryArithmetic.Sub8(processor.Registers, operand),
                3 => BinaryArithmetic.Sub8(processor.Registers, operand, true),
                4 => BinaryArithmetic.And(processor.Registers, operand),
                5 => BinaryArithmetic.Xor(processor.Registers, operand),
                6 => BinaryArithmetic.Or(processor.Registers, operand),
                7 => BinaryArithmetic.Compare(processor.Registers, operand),
                _ => throw new UnreachableException()
            };

            return 0;
        });
        
        // Block 3 - The unused opcodes are caught by the node; this is an exception to the unreachable group node rule.
        interpreter.AddInstruction("11xxxxxx", (_, _, _, _) 
            => 0);
        
        // alu a,u8
        interpreter.AddInstruction("11ooo110", (processor, bus, pattern, opcode) =>
        {
            byte operand = bus.ReadByte(processor.Registers.Pc++);
            
            processor.Registers = pattern.Variables['o'].Extract(opcode) switch
            {
                0 => BinaryArithmetic.Add8(processor.Registers, operand),
                1 => BinaryArithmetic.Add8(processor.Registers, operand, true),
                2 => BinaryArithmetic.Sub8(processor.Registers, operand),
                3 => BinaryArithmetic.Sub8(processor.Registers, operand, true),
                4 => BinaryArithmetic.And(processor.Registers, operand),
                5 => BinaryArithmetic.Xor(processor.Registers, operand),
                6 => BinaryArithmetic.Or(processor.Registers, operand),
                7 => BinaryArithmetic.Compare(processor.Registers, operand),
                _ => throw new UnreachableException()
            };

            return 0;
        });
        
        // ret cc
        interpreter.AddInstruction("110cc000", (processor, bus, pattern, opcode) =>
        {
            int cc = pattern.Variables['c'].Extract(opcode);
            if (!InstructionOperandLookup.EvalConditional(processor.Registers, cc))
                return 0;

            processor.Registers.Pc = Pop(processor, bus);
            return 12;
        });
        
        // ret
        interpreter.AddInstruction("11001001", (processor, bus, _, _) =>
        {
            processor.Registers.Pc = Pop(processor, bus);
            return 0;
        });
        
        // reti - todo: Implement interrupt behavior
        interpreter.AddInstruction("11011001", (processor, bus, _, _) =>
        {
            processor.Registers.Pc = Pop(processor, bus);
            return 0;
        });
        
        // jp cc,u16
        interpreter.AddInstruction("110cc010", (processor, bus, pattern, opcode) =>
        {
            int cc = pattern.Variables['c'].Extract(opcode);
            byte low = bus.ReadByte(processor.Registers.Pc++);
            byte high = bus.ReadByte(processor.Registers.Pc++);
            
            if (!InstructionOperandLookup.EvalConditional(processor.Registers, cc))
                return 0;
            
            processor.Registers.Pc = (ushort)((high << 8) | low);
            return 4;
        });
        
        // jp u16
        interpreter.AddInstruction("11000011", (processor, bus, _, _) =>
        {
            byte low = bus.ReadByte(processor.Registers.Pc++);
            byte high = bus.ReadByte(processor.Registers.Pc++);
            processor.Registers.Pc = (ushort)((high << 8) | low);

            return 0;
        });
        
        interpreter.AddInstruction("11101001", (processor, _, _, _) =>
        {
            processor.Registers.Pc = processor.Registers.HL;
            return 0;
        });
        
        // call cc,u16
        interpreter.AddInstruction("110cc100", (processor, bus, pattern, opcode) =>
        {
            int cc =  pattern.Variables['c'].Extract(opcode);
            byte low = bus.ReadByte(processor.Registers.Pc++);
            byte high = bus.ReadByte(processor.Registers.Pc++);
            
            if (!InstructionOperandLookup.EvalConditional(processor.Registers, cc))
                return 0;
            
            ushort dst = (ushort)((high << 8) | low);

            Push(processor, bus, processor.Registers.Pc);
            processor.Registers.Pc = dst;
            
            return 12;
        });
        
        // call u16
        interpreter.AddInstruction("11001101", (processor, bus, _, _) =>
        {
            byte low = bus.ReadByte(processor.Registers.Pc++);
            byte high = bus.ReadByte(processor.Registers.Pc++);
            ushort dst = (ushort)((high << 8) | low);

            Push(processor, bus, processor.Registers.Pc);
            processor.Registers.Pc = dst;
            
            return 0;
        });
        
        // rst vec
        interpreter.AddInstruction("11vvv111", (processor, bus, pattern, opcode) =>
        {
            ushort vec = (ushort)(pattern.Variables['v'].Extract(opcode) * 8);
            Push(processor, bus, processor.Registers.Pc);
            processor.Registers.Pc = vec;
            
            return 0;
        });
        
        // pop r16
        interpreter.AddInstruction("11rr0001", (processor, bus, pattern, opcode) =>
        {
            R16 reg = InstructionOperandLookup.GetR16Stack(pattern.Variables['r'].Extract(opcode));
            processor.Registers[reg] = Pop(processor, bus);

            return 0;
        });
        
        // push r16
        interpreter.AddInstruction("11rr0101", (processor, bus, pattern, opcode) =>
        { 
            R16 reg = InstructionOperandLookup.GetR16Stack(pattern.Variables['r'].Extract(opcode));
            Push(processor, bus, processor.Registers[reg]);

            return 0;
        });
        
        // prefix cb
        interpreter.AddInstruction("11001011", (_, _, _, _) 
            => throw new UnreachableException());
        
        // ldh [c],a
        interpreter.AddInstruction("11100010", (processor, bus, _, _) =>
        {
            bus.WriteByte((ushort)(0xFF00 + processor.Registers.C), processor.Registers.A);
            return 0;
        });
        
        // ldh [u8],a
        interpreter.AddInstruction("11100000", (processor, bus, _, _) =>
        {
            byte offset = bus.ReadByte(processor.Registers.Pc++);
            
            bus.WriteByte((ushort)(0xFF00 + offset), processor.Registers.A);
            return 0;
        });
        
        // ld [u16],a
        interpreter.AddInstruction("11101010", (processor, bus, _, _) =>
        {
            byte low = bus.ReadByte(processor.Registers.Pc++);
            byte high = bus.ReadByte(processor.Registers.Pc++);
            ushort dst = (ushort)((high << 8) | low);

            bus.WriteByte(dst, processor.Registers.A);
            return 0;
        });
        
        // ldh a,[c]
        interpreter.AddInstruction("11110010", (processor, bus, _, _) =>
        {
            processor.Registers.A = bus.ReadByte((ushort)(0xFF00 + processor.Registers.C));
            return 0;
        });
        
        // ldh a,[u8]
        interpreter.AddInstruction("11110000", (processor, bus, _, _) =>
        {
            byte offset = bus.ReadByte(processor.Registers.Pc++);
            processor.Registers.A = bus.ReadByte((ushort)(0xFF00 + offset));

            return 0;
        });
        
        // ld a,[u16]
        interpreter.AddInstruction("11111010", (processor, bus, _, _) =>
        {
            byte low = bus.ReadByte(processor.Registers.Pc++);
            byte high = bus.ReadByte(processor.Registers.Pc++);
            ushort src = (ushort)((high << 8) | low);
            
            processor.Registers.A = bus.ReadByte(src);
            return 0;
        });
        
        // add sp,i8
        interpreter.AddInstruction("11101000", (processor, bus, _, _) =>
        {
            sbyte operand = (sbyte)bus.ReadByte(processor.Registers.Pc++);

            processor.Registers.FlagZ = false;
            processor.Registers.FlagN = false;
            processor.Registers.FlagH = (((processor.Registers.Sp & 0x0F) + (operand & 0x0F)) & 0x10) == 0x10;
            processor.Registers.FlagC = (((processor.Registers.Sp & 0xFF) + (operand & 0xFF)) & 0x100) == 0x100;
            processor.Registers.Sp = (ushort)(processor.Registers.Sp + operand);

            return 0;
        });
        
        // ld hl,sp+i8
        interpreter.AddInstruction("11111000", (processor, bus, _, _) =>
        {
            sbyte operand = (sbyte)bus.ReadByte(processor.Registers.Pc++);

            processor.Registers.FlagZ = false;
            processor.Registers.FlagN = false;
            processor.Registers.FlagH = (((processor.Registers.Sp & 0x0F) + (operand & 0x0F)) & 0x10) == 0x10;
            processor.Registers.FlagC = (((processor.Registers.Sp & 0xFF) + (operand & 0xFF)) & 0x100) == 0x100;
            processor.Registers.HL = (ushort)(processor.Registers.Sp + operand);

            return 0;
        });
        
        // ld sp, hl
        interpreter.AddInstruction("11111001", (processor, _, _, _) =>
        {
            processor.Registers.Sp = processor.Registers.HL;
            return 0;
        });
        
        // di todo: implement
        interpreter.AddInstruction("11110011", (_, _, _, _) => 0);

        // ei todo: implement
        interpreter.AddInstruction("11111011", (_, _, _, _) => 0);

        // CB PREFIX
        // shift operations
        interpreter.AddInstruction("00ooorrr", (processor, _, pattern, opcode) =>
        {
            R8 r8 = InstructionOperandLookup.GetR8(pattern.Variables['r'].Extract(opcode));

            processor.Registers = pattern.Variables['o'].Extract(opcode) switch
            {
                0 => BinaryArithmetic.Rlc(processor.Registers, r8),
                1 => BinaryArithmetic.Rrc(processor.Registers, r8), 
                2 => BinaryArithmetic.Rl(processor.Registers, r8),
                3 => BinaryArithmetic.Rr(processor.Registers, r8),
                4 => BinaryArithmetic.Sla(processor.Registers, r8),
                5 => BinaryArithmetic.Sra(processor.Registers, r8),
                6 => BinaryArithmetic.Swap(processor.Registers, r8),
                7 => BinaryArithmetic.Srl(processor.Registers, r8),
                _ => throw new UnreachableException()
            };

            return 0;
        }, 1);
        interpreter.AddInstruction("00ooo110", (processor, bus, pattern, opcode) =>
        {
            byte operand = bus.ReadByte(processor.Registers.HL);

            (processor.Registers, operand) = pattern.Variables['o'].Extract(opcode) switch
            {
                0 => BinaryArithmetic.Rlc(processor.Registers, operand),
                1 => BinaryArithmetic.Rrc(processor.Registers, operand), 
                2 => BinaryArithmetic.Rl(processor.Registers, operand),
                3 => BinaryArithmetic.Rr(processor.Registers, operand),
                4 => BinaryArithmetic.Sla(processor.Registers, operand),
                5 => BinaryArithmetic.Sra(processor.Registers, operand),
                6 => BinaryArithmetic.Swap(processor.Registers, operand),
                7 => BinaryArithmetic.Srl(processor.Registers, operand),
                _ => throw new UnreachableException()
            };

            bus.WriteByte(processor.Registers.HL, operand);
            
            return 0;
        }, 1);
        
        // bit test
        interpreter.AddInstruction("01bbbooo", (processor, bus, pattern, opcode) =>
        {
            byte operand = (byte)pattern.Variables['o'].Extract(opcode);
            operand = operand switch
            {
                <= 5 or 7 => processor.Registers[InstructionOperandLookup.GetR8(operand)],
                6 => bus.ReadByte(processor.Registers.HL),
                _ => throw new UnreachableException()
            };

            processor.Registers =
                BinaryArithmetic.Test(processor.Registers, operand, pattern.Variables['b'].Extract(opcode));
            return 0;
        }, 1);
        
        // bit changing
        interpreter.AddInstruction("1vbbbooo", (processor, _, pattern, opcode) =>
        {
            R8 r = InstructionOperandLookup.GetR8(pattern.Variables['o'].Extract(opcode));
            int bit = pattern.Variables['b'].Extract(opcode);
            bool value = pattern.Variables['v'].Extract(opcode) == 1;

            processor.Registers[r] = BinaryHelper.SetBitForInteger(processor.Registers[r], bit, value);
            
            return 0;
        }, 1);
        interpreter.AddInstruction("1vbbb110", (processor, bus, pattern, opcode) =>
        {
            byte operand = bus.ReadByte(processor.Registers.HL);
            int bit = pattern.Variables['b'].Extract(opcode);
            bool value = pattern.Variables['v'].Extract(opcode) == 1;

            bus.WriteByte(processor.Registers.HL, BinaryHelper.SetBitForInteger(operand, bit, value));
            
            return 0;  
        }, 1);
        
        return interpreter;

        int Group(Processor proc, IBusConnection bus, BytePattern pattern, byte opcode) =>
            throw new UnreachableException("Group nodes should be unreachable");

        void Push(Processor proc, IBusConnection bus, ushort value)
        {
            byte high = (byte)(value >> 8);
            bus.WriteByte(--proc.Registers.Sp,  high);
            bus.WriteByte(--proc.Registers.Sp, (byte)value);
        }

        ushort Pop(Processor proc, IBusConnection bus)
        {
            byte low = bus.ReadByte(proc.Registers.Sp++);
            byte high = bus.ReadByte(proc.Registers.Sp++);
            
            return (ushort)((high << 8) | low);
        }
    }
}
using System.Text.Json;
using Dgb.ByteDecoder;

namespace DgbTest;

/*
 * The purpose of this test is to ensure that the decoder tree is capable of parsing the entire Gameboy
 * instruction set assuming that the tree itself is properly set up. This is achieved by generating
 * the pneumonic for every opcode programatically and then comparing the results to a known good list
 */
public class ByteDecoderTest
{
    private GeneratorCollection _primaryTable;
    private GeneratorCollection _prefixedTable;
    private List<string> _correctNames;
    private List<string> _correctNamesCb;

    private string BlockCall(BytePattern pattern, byte opcode) => "unused";
    
    private string GetR16A(int r) => r switch
    {
        0 => "bc",
        1 => "de",
        2 => "hl",
        3 => "sp",
        _ => throw new NotImplementedException()
    };
        
    private string GetR16B(int r) => r switch
    {
        0 => "bc",
        1 => "de",
        2 => "hl+",
        3 => "hl-",
        _ => throw new NotImplementedException()
    };
        
    private string GetR16C(int r) => r switch
    {
        0 => "bc",
        1 => "de",
        2 => "hl",
        3 => "af",
        _ => throw new NotImplementedException()
    };

    private string GetR8(int r) => r switch
    {
        0 => "b",
        1 => "c",
        2 => "d",
        3 => "e",
        4 => "h",
        5 => "l",
        6 => "(hl)",
        7 => "a",
        _ => throw new NotImplementedException()
    };

    private string GetCC(int c) => c switch
    {
        0 => "nz",
        1 => "z",
        2 => "nc",
        3 => "c",
        _ => throw new NotImplementedException()
    };

    private void GeneratePrimaryTree()
    {
        // Creating the name generating tree //
        
        /*
         * Block 0
         */
        _primaryTable.AddPattern("00xxxxxx", BlockCall);

        // nop
        _primaryTable.AddPattern("00000000", (_, _) => "nop");
        // ld r16, u16
        _primaryTable.AddPattern("00dd0001", (p, o) =>
        {
            string rName = GetR16A(p.Variables['d'].Extract(o)) ;
            return $"ld {rName},u16";
        });
        // ld (r16), a
        _primaryTable.AddPattern("00dd0010", (p, o) =>
        {
            string rName = GetR16B(p.Variables['d'].Extract(o));
            return $"ld ({rName}),a";
        });
        // ld a, (r16)
        _primaryTable.AddPattern("00ss1010", (p, o) =>
        {
            string rName = GetR16B(p.Variables['s'].Extract(o));
            return $"ld a,({rName})";
        });
        // ld [u16], sp
        _primaryTable.AddPattern("00001000", (_, _) => "ld (u16),sp");
        // inc r16
        _primaryTable.AddPattern("00xx0011", (p, o) =>
        {
            string rName = GetR16A(p.Variables['x'].Extract(o));
            return $"inc {rName}";
        });
        // dec r16
        _primaryTable.AddPattern("00xx1011", (p, o) =>
        {
            string rName = GetR16A(p.Variables['x'].Extract(o));
            return $"dec {rName}";
        });
        // add hl, r16
        _primaryTable.AddPattern("00xx1001", (p, o) =>
        {
            string rName = GetR16A(p.Variables['x'].Extract(o));
            return $"add hl,{rName}";
        });
        // inc r8
        _primaryTable.AddPattern("00xxx100",(p, o) =>
        {
            string rName = GetR8(p.Variables['x'].Extract(o));
            return $"inc {rName}";
        });
        // dec r8
        _primaryTable.AddPattern("00xxx101",(p, o) =>
        {
            string rName = GetR8(p.Variables['x'].Extract(o));
            return $"dec {rName}";
        });
        // ld r8, u8
        _primaryTable.AddPattern("00ddd110",(p, o) =>
        {
            string rName = GetR8(p.Variables['d'].Extract(o));
            return $"ld {rName},u8";
        });
        // rlca
        _primaryTable.AddPattern("00000111", (_, _) => "rlca");
        // rrca
        _primaryTable.AddPattern("00001111", (_, _) => "rrca");
        // rla 
        _primaryTable.AddPattern("00010111", (_, _) => "rla");
        // rra
        _primaryTable.AddPattern("00011111", (_, _) => "rra");
        // daa
        _primaryTable.AddPattern("00100111",  (_, _) => "daa");
        // cpl
        _primaryTable.AddPattern("00101111", (_, _) => "cpl");
        // scf
        _primaryTable.AddPattern("00110111", (_, _) => "scf");
        // ccf 
        _primaryTable.AddPattern("00111111", (_, _) => "ccf");
        // jr i8
        _primaryTable.AddPattern("00011000", (_, _) => "jr i8");
        // jr cc, i8
        _primaryTable.AddPattern("001cc000", (p, o) =>
        {
            string cName = GetCC(p.Variables['c'].Extract(o));
            return $"jr {cName},i8";
        });
        // stop
        _primaryTable.AddPattern("00010000", (_, _) => "stop");
        
        
        /*
         * Block 1
         */
        _primaryTable.AddPattern("01dddsss", (p, o) =>
        {
            string dst =  GetR8(p.Variables['d'].Extract(o));
            string src = GetR8(p.Variables['s'].Extract(o));
            return $"ld {dst},{src}";
        });
        // halt
        _primaryTable.AddPattern("01110110", (_, _) => "halt");
        
        /*
         * Block 2
         */
        _primaryTable.AddPattern("10xxxxxx", BlockCall);
        // add a, r8
        _primaryTable.AddPattern("10000xxx", (p, o) =>
        {
            string rName = GetR8(p.Variables['x'].Extract(o));
            return $"add a,{rName}";
        });
        // adc a, r8
        _primaryTable.AddPattern("10001xxx", (p, o) =>
        {
            string rName = GetR8(p.Variables['x'].Extract(o));
            return $"adc a,{rName}";
        });
        // sub a, r8
        _primaryTable.AddPattern("10010xxx", (p, o) =>
        {
            string rName = GetR8(p.Variables['x'].Extract(o));
            return $"sub a,{rName}";
        });
        // sbc a, r8
        _primaryTable.AddPattern("10011xxx", (p, o) =>
        {
            string rName = GetR8(p.Variables['x'].Extract(o));
            return $"sbc a,{rName}";
        });
        // and a, r8
        _primaryTable.AddPattern("10100xxx", (p, o) =>
        {
            string rName = GetR8(p.Variables['x'].Extract(o));
            return $"and a,{rName}";
        });
        // xor a, r8
        _primaryTable.AddPattern("10101xxx", (p, o) =>
        {
            string rName = GetR8(p.Variables['x'].Extract(o));
            return $"xor a,{rName}";
        });
        // or a, r8
        _primaryTable.AddPattern("10110xxx", (p, o) =>
        {
            string rName = GetR8(p.Variables['x'].Extract(o));
            return $"or a,{rName}";
        });
        // cp a, r8
        _primaryTable.AddPattern("10111xxx", (p, o) =>
        {
            string rName = GetR8(p.Variables['x'].Extract(o));
            return $"cp a,{rName}";
        });
        
        /*
         * Block 3
         */
        _primaryTable.AddPattern("11xxxxxx", BlockCall);
        // add a, u8
        _primaryTable.AddPattern("11000110", (_, _) => "add a,u8");
        // adc a, u8
        _primaryTable.AddPattern("11001110", (_, _) => "adc a,u8");
        // sub a, u8
        _primaryTable.AddPattern("11010110", (_, _) => "sub a,u8");
        // sbc a, u8
        _primaryTable.AddPattern("11011110", (_, _) => "sbc a,u8");
        // and a, u8
        _primaryTable.AddPattern("11100110", (_, _) => "and a,u8");
        // xor a, u8
        _primaryTable.AddPattern("11101110", (_, _) => "xor a,u8");
        // or a, u8
        _primaryTable.AddPattern("11110110", (_, _) => "or a,u8");
        // cp a, u8
        _primaryTable.AddPattern("11111110", (_, _) => "cp a,u8");
        // ret cc
        _primaryTable.AddPattern("110cc000", (p, o) =>
        {
            string cName = GetCC(p.Variables['c'].Extract(o));
            return $"ret {cName}";
        });
        // ret
        _primaryTable.AddPattern("11001001", (_, _) => "ret");
        // reti
        _primaryTable.AddPattern("11011001", (_, _) => "reti");
        // jp cc, u16
        _primaryTable.AddPattern("110cc010", (p, o) =>
        {
            string cName = GetCC(p.Variables['c'].Extract(o));
            return $"jp {cName},u16";
        });
        // jp u16
        _primaryTable.AddPattern("11000011", (_, _) => "jp u16");
        // jp hl
        _primaryTable.AddPattern("11101001",  (_, _) => "jp hl");
        // call cc u16
        _primaryTable.AddPattern("110cc100", (p, o) =>
        {
            string cName = GetCC(p.Variables['c'].Extract(o));
            return $"call {cName},u16";
        });
        // call u16
        _primaryTable.AddPattern("11001101",  (_, _) => "call u16");
        // rst vec
        _primaryTable.AddPattern("11vvv111", (p, o) =>
        {
            int vec = p.Variables['v'].Extract(o) * 8;
            return $"rst {vec:x2}h";
        });
        // pop r16
        _primaryTable.AddPattern("11rr0001", (p, o) =>
        {
            string rName = GetR16C(p.Variables['r'].Extract(o));
            return $"pop {rName}";
        });
        // push r16
        _primaryTable.AddPattern("11rr0101", (p, o) =>
        {
            string rName = GetR16C(p.Variables['r'].Extract(o));
            return $"push {rName}";
        });
        // cb prefix
        _primaryTable.AddPattern("11001011", (_, _) => "prefix cb");
        // ldh [c], a
        _primaryTable.AddPattern("11100010", (_, _) => "ld (ff00+c),a");
        // ldh [u8], a 
        _primaryTable.AddPattern("11100000", (_, _) => "ld (ff00+u8),a");
        // ld [u16], a
        _primaryTable.AddPattern("11101010", (_, _) => "ld (u16),a");
        // ldh a, [c] 
        _primaryTable.AddPattern("11110010", (_, _) => "ld a,(ff00+c)");
        // ldh a, [u8]
        _primaryTable.AddPattern("11110000",  (_, _) => "ld a,(ff00+u8)");
        // ld a, [u16]
        _primaryTable.AddPattern("11111010",  (_, _) => "ld a,(u16)");
        // add sp, u8 - 00HC
        _primaryTable.AddPattern("11101000", (_, _) => "add sp,i8");
        // ld hl, sp+u8 - 00HC
        _primaryTable.AddPattern("11111000",  (_, _) => "ld hl,sp+i8");
        // ld sp, hl
        _primaryTable.AddPattern("11111001",   (_, _) => "ld sp,hl");
        // di
        _primaryTable.AddPattern("11110011", (_, _) => "di");
        // ei
        _primaryTable.AddPattern("11111011",   (_, _) => "ei");
    }

    private void GeneratePrefixedTree()
    {
        _prefixedTable.AddPattern("00xxxxxx", BlockCall);
        _prefixedTable.AddPattern("00000rrr", (pattern, opcode) => 
            $"rlc {GetR8(pattern.Variables['r'].Extract(opcode))}");
        _prefixedTable.AddPattern("00001rrr", (pattern, opcode) => 
            $"rrc {GetR8(pattern.Variables['r'].Extract(opcode))}");
        _prefixedTable.AddPattern("00010rrr", (pattern, opcode) => 
            $"rl {GetR8(pattern.Variables['r'].Extract(opcode))}");
        _prefixedTable.AddPattern("00011rrr", (pattern, opcode) => 
            $"rr {GetR8(pattern.Variables['r'].Extract(opcode))}");
        _prefixedTable.AddPattern("00100rrr", (pattern, opcode) => 
            $"sla {GetR8(pattern.Variables['r'].Extract(opcode))}");
        _prefixedTable.AddPattern("00101rrr", (pattern, opcode) => 
            $"sra {GetR8(pattern.Variables['r'].Extract(opcode))}");
        _prefixedTable.AddPattern("00110rrr", (pattern, opcode) => 
            $"swap {GetR8(pattern.Variables['r'].Extract(opcode))}");
        _prefixedTable.AddPattern("00111rrr", (pattern, opcode) => 
            $"srl {GetR8(pattern.Variables['r'].Extract(opcode))}");

        _prefixedTable.AddPattern("01bbbrrr", (pattern, opcode) =>
            $"bit {pattern.Variables['b'].Extract(opcode)},{GetR8(pattern.Variables['r'].Extract(opcode))}");
        _prefixedTable.AddPattern("10bbbrrr", (pattern, opcode) =>
            $"res {pattern.Variables['b'].Extract(opcode)},{GetR8(pattern.Variables['r'].Extract(opcode))}");
        _prefixedTable.AddPattern("11bbbrrr", (pattern, opcode) =>
            $"set {pattern.Variables['b'].Extract(opcode)},{GetR8(pattern.Variables['r'].Extract(opcode))}");
    }
    
    [OneTimeSetUp]
    public void Setup()
    {
        // Var setup
        _primaryTable = new GeneratorCollection();
        _prefixedTable = new GeneratorCollection();
        
        // Loading the correct names into memory //

        JsonDocument js = JsonDocument.Parse(File.ReadAllText("dmgops.json"));
        _correctNames = js.RootElement.GetProperty("Unprefixed").EnumerateArray()
            .Select(e => e.GetProperty("Name").ToString()).ToList();
        _correctNamesCb = js.RootElement.GetProperty("CBPrefixed").EnumerateArray()
            .Select(e => e.GetProperty("Name").ToString()).ToList();
        
        GeneratePrimaryTree();
        GeneratePrefixedTree();
    }

    [Test]
    public void UnprefixedTest()
    {
        for (int b = 0; b <= 0xFF; b++)
        {
            string generated = _primaryTable.GenerateName((byte)b);
            string correct = _correctNames[b].ToLower();
            Assert.That(generated == correct, $"Opcode mismatch: {b:X2} - {generated}:{correct}");
        }    
    }

    [Test]
    public void PrefixedTest()
    {
        for (int b = 0; b <= 0xFF; b++)
        {
            string generated = _prefixedTable.GenerateName((byte)b);
            string correct = _correctNamesCb[b].ToLower();
            Assert.That(generated == correct, $"Opcode mismatch: {b:X2} - {generated}:{correct}");
        }    
    }
}
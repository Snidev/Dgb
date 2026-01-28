using System.Collections.Frozen;
using System.Runtime.InteropServices;

namespace Dgb.ByteDecoder;

public class BytePattern
{
    public readonly byte ConstMask;
    public readonly byte ConstVal;
    public readonly string Pattern;
    public readonly FrozenDictionary<char, VariableInfo> Variables;
    public readonly bool IsConstant;
    
    public bool Test(byte value) => (value & ConstMask) == ConstVal;

    // This constructor needs to be a lot neater, but it works for now
    public BytePattern(string pattern)
    {
        // Ideally in the future allow ignored characters such as _ or other whitespaces
        if (pattern.Length != 8)
            throw new BytePatternParserException("Pattern length must be 8");
        
        char? curVar = null;
        byte constMask = 0;
        byte constVal = 0;
        // Not sure if something like a sparse set would be faster in this case as we're effectively 
        // mapping an int to an int, but a frozen dictionary should be more than fast enough
        Dictionary<char, VariableInfo> vars = new();

        // Parse the string to extract variable bits as well as constant bits
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            int bitPos = 7 - i;
            if (curVar == null)
            {
                if (c == '0')
                    constMask |= (byte)(1 << bitPos);
                else if (c == '1')
                {
                    constMask |= (byte)(1 << bitPos);
                    constVal |= (byte)(1 << bitPos);
                }
                else if (c is >= 'a' and <= 'z')
                {
                    curVar = c;
                    vars.Add(c, new VariableInfo(c, bitPos, 1, (byte)(1 << bitPos)));
                }
            }
            else
            {
                if (c == curVar)
                {
                    VariableInfo v = vars[c];
                    vars[c] = new VariableInfo(v.Name, v.Start, v.Length + 1, (byte)(v.Mask | (byte)(1 << bitPos)));
                }
                else if (c is >= 'a' and <= 'z')
                {
                    curVar = c;
                    vars.Add(c, new VariableInfo(c, bitPos, 1, (byte)(1 << bitPos)));
                }
                else
                {
                    curVar = null;
                    if (c == '0')
                        constMask |= (byte)(1 << bitPos);
                    else if (c == '1')
                    {
                        constMask |= (byte)(1 << bitPos);
                        constVal |= (byte)(1 << bitPos);
                    }
                }
            }
        }
        
        ConstMask = constMask;
        ConstVal = constVal;
        Pattern = pattern;
        Variables = vars.ToFrozenDictionary();
        IsConstant = ConstMask == 0xFF;
    }
}

public class BytePatternParserException : Exception
{
    public BytePatternParserException()
    {
    }

    public BytePatternParserException(string message) : base(message)
    {
    }

    public BytePatternParserException(string message, Exception inner) : base(message, inner)
    {
    }
}
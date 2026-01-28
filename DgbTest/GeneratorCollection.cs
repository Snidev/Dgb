using Dgb.ByteDecoder;

namespace DgbTest;

public class GeneratorCollection
{
    public delegate string NameGenerator(BytePattern pattern, byte opcode);
    private PatternTree _patternTree = new();
    private Dictionary<string, NameGenerator> _nameGenerator = new();

    public void AddPattern(string pattern, NameGenerator generator)
    {
        _patternTree.AddPattern(pattern);
        _nameGenerator.Add(pattern, generator);
    }

    public string GenerateName(byte opcode)
    {
        if (!_patternTree.Test(opcode, out BytePattern? pattern))
            return "";

        return _nameGenerator.TryGetValue(pattern.Pattern, out NameGenerator? generator)
            ? generator.Invoke(pattern, opcode)
            : "";
    }
}
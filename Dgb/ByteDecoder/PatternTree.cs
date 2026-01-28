using System.Diagnostics.CodeAnalysis;
using Dgb.Core.Cpu;

namespace Dgb.ByteDecoder;

// Possible optimization: Frozen version of collection using contiguous storage
public class PatternTree
{
    public delegate void Callback(Processor proc, BytePattern pattern, byte opcode);
    
    private Dictionary<byte, BytePattern> _constants = new();
    private List<Node> _heads = new();
    
    public bool Test(byte value, [NotNullWhen(true)] out BytePattern? pattern)
    {
        if (_constants.TryGetValue(value, out pattern))
            return true;

        foreach (Node child in _heads)
            if (child.Test(value, out BytePattern? p))
            {
                pattern = p;
                return true;
            }
        
        pattern = null;
        return false;
    }

    public void AddPattern(string pattern)
    {
        BytePattern bp = new(pattern);

        if (bp.IsConstant)
        {
            _constants.Add(bp.ConstVal, bp);
            return;
        }
        
        Node node = new(bp);
        
        foreach (Node child in _heads)
            if (child.Insert(node))
                return;
        
        _heads.Add(node);
        
        List<Node> removals = new();
        foreach (Node child in _heads)
            if (node.Insert(child))
                removals.Add(child);

        foreach (Node removal in removals)
            _heads.Remove(removal);
    }
    
    private class Node(BytePattern pattern)
    {
        public readonly BytePattern Pattern = pattern;
        private List<Node> _children = [];

        public bool IsSame(Node node) =>
            Pattern.ConstMask == node.Pattern.ConstMask && Pattern.ConstVal == node.Pattern.ConstVal;

        public bool IsSuperPattern(Node node) =>
            !IsSame(node) && Pattern.ConstMask == (node.Pattern.ConstMask & Pattern.ConstMask) &&
            Pattern.ConstVal == (node.Pattern.ConstVal & Pattern.ConstMask);

        public bool Insert(Node node)
        {
            if (!IsSuperPattern(node))
                return false;

            foreach (Node child in _children)
                if (child.Insert(node))
                    return true;
            
            List<Node> removals = new();
            foreach (Node child in _children)
                if (node.Insert(child))
                    removals.Add(child);

            foreach (Node removal in removals)
                _children.Remove(removal);

            _children.Add(node);
            
            return true;
        }
        
        public bool Test(byte value, [NotNullWhen(true)]out BytePattern? pattern)
        {
            if (Pattern.Test(value))
                pattern = Pattern;
            else
            {
                pattern = null;
                return false;
            }

            foreach (Node child in _children)
                if (child.Test(value, out BytePattern? p))
                {
                    pattern = p;
                    return true;
                }

            return true;
        }
    }
}
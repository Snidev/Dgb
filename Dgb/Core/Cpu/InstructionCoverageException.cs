namespace Dgb.Core.Cpu;

public class InstructionCoverageException : Exception
{
    public InstructionCoverageException()
    {
    }

    public InstructionCoverageException(string message) : base(message)
    {
    }

    public InstructionCoverageException(string message, Exception inner) : base(message, inner)
    {
    }
}
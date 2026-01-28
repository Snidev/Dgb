using Dgb.Core.Bus;

namespace Dgb.Core.Cpu;

public class Processor(IBusConnection bus)
{
    public Registers Registers = new();
    private readonly uint _executionTime = 0;
    private readonly IBusConnection _bus = bus;
    private readonly Interpreter _interpreter = Interpreter.CreateDmgInterpreter();
    
    
}
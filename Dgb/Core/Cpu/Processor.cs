#define CpuTimeTracking

using System.Diagnostics;
using System.Text.Json;
using Dgb.Core.Bus;

namespace Dgb.Core.Cpu;

public class Processor
{
    public Registers Registers = new();
    public bool IsReadyForInstruction => _executionTime != 0;
    private uint _executionTime = 0;
    private readonly IBusConnection _bus;
    private readonly Interpreter _interpreter = Interpreter.CreateDmgInterpreter();
    private readonly int[][] _timingTable;
    
    public void Tick()
    {
        if (!IsReadyForInstruction)
        {
            _executionTime--;
            return;
        }
        
        byte opcode = _bus.ReadByte(Registers.Pc++);
        
        if (opcode == 0xCB)
        {
            opcode = _bus.ReadByte(Registers.Pc++);
            _executionTime = (uint)_timingTable[1][opcode] +
                          (uint)_interpreter.Execute(this, _bus, opcode, out _, 1);
        }
        else
            _executionTime = (uint)_timingTable[0][opcode] +  
                (uint)_interpreter.Execute(this, _bus, opcode, out _);
        
    }
    
    public Processor(IBusConnection bus)
    {
        _bus = bus;

        using FileStream stream = File.OpenRead(@"Data\cycles.json");
        using JsonDocument timingDocument = JsonDocument.Parse(stream);

        JsonElement root = timingDocument.RootElement;
        
        int[] unpfx = root.GetProperty("UnprefixedTCycles")
            .EnumerateArray()
            .Select(e => e.GetInt32())
            .ToArray();

        int[] pfx = root.GetProperty("CBPrefixedTCycles")
            .EnumerateArray()
            .Select(e => e.GetInt32())
            .ToArray();

        _timingTable = [unpfx, pfx];
    }
}
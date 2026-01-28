using Dgb.ByteDecoder;
using Dgb.Core.Bus;
using Dgb.Core.Cpu;

namespace DgbTest;

public class TestRoms
{
    private BasicBus _bus;
    private Processor _processor;
    private Interpreter _interpreter;

    private static IEnumerable<string> TestData()
        => Directory.EnumerateFiles("TestData", "*.gb").Select(Path.GetFileNameWithoutExtension)!;

    [OneTimeSetUp]
    public void GlobalSetup()
    {
        _interpreter = Interpreter.CreateDmgInterpreter();
    }

    [SetUp]
    public void Setup()
    {
        _bus = new BasicBus();
        _processor = new Processor(_bus);

        _processor.Registers.Pc = 0x100;
        _processor.Registers.AF = 0x01B0;
        _processor.Registers.BC = 0x0013;
        _processor.Registers.DE = 0x00D8;
        _processor.Registers.HL = 0x014D;
        _processor.Registers.Sp = 0xFFFE;
    }

    [TestCaseSource(nameof(TestData))]
    public void TestRom(string testName)
    {
        File.ReadAllBytes(Path.Combine("TestData", testName + ".gb")).CopyTo(_bus.Data);
        StreamReader logFile = new (File.OpenRead(Path.Combine("TestData", testName + ".txt")));
        string? goodLog = logFile.ReadLine();
        string? prevGood = null;
        BytePattern? lastPattern = null;
        ulong lineNumber = 1;
        
        while(goodLog != null)
        {
            string execResult = GetStateStr(_processor, _bus);
            
            // Test logs do not include code in bootrom
            if (_processor.Registers.Pc >= 0x100)
                Assert.That(goodLog, Is.EqualTo(execResult), 
                (prevGood != null ? $"Log file line {lineNumber-1}\n\t\t\t{prevGood}\nPattern: {lastPattern!.Pattern}\n" : "") +
                $"Log file line {lineNumber}\nExpected:\t{goodLog}\nResult:\t\t{execResult}");
            
            byte opcode = _bus.ReadByte(_processor.Registers.Pc++);
            if (opcode == 0xCB)
            {
                opcode = _bus.ReadByte(_processor.Registers.Pc++);
                _interpreter.Execute(_processor, _bus, opcode, out lastPattern, 1);
            }
            else
                _interpreter.Execute(_processor, _bus, opcode, out lastPattern);


            if (_processor.Registers.Pc >= 0x100){
                lineNumber++;
                prevGood = goodLog;
                goodLog = logFile.ReadLine();
            }
        }
    }

    private string GetStateStr(Processor proc, IBusConnection bus) =>
            $"A: {proc.Registers.A:X2} F: {proc.Registers.F:X2} B: {proc.Registers.B:X2} C: {proc.Registers.C:X2} D: {proc.Registers.D:X2} E: {proc.Registers.E:X2} H: {proc.Registers.H:X2} L: {proc.Registers.L:X2} SP: {proc.Registers.Sp:X4} PC: 00:{proc.Registers.Pc:X4} ({_bus.ReadByte(proc.Registers.Pc):X2} {_bus.ReadByte((ushort)(proc.Registers.Pc + 1)):X2} {_bus.ReadByte((ushort)(proc.Registers.Pc + 2)):X2} {_bus.ReadByte((ushort)(proc.Registers.Pc + 3)):X2})";
}
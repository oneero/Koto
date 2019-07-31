using Godot;
using System;
//using static Globals.Logger;

public class Testbench : Node
{
    [Export]
    public NodePath loggerPath;
    private Logger logger;

    [Export]
    public NodePath sourcePath;
    private TextEdit sourceInput;

    private VM vm;
    private Disassembler disassembler;
    private Chunk chunk;

    private bool runRepl = false;

    public override void _Ready()
    {
        GD.Print("Testbench ready");

        logger = GetNode(loggerPath) as Logger;

        sourceInput = GetNode(sourcePath) as TextEdit;

        vm = new VM(logger);

        /* disassembler = new Disassembler();
        chunk = new Chunk();
        CreateTestChunk(chunk);
        //DissasembleChunk(chunk);
        InterpretChunk(chunk); */
    }

    public void _on_Button_pressed()
    {
        RunSource(sourceInput.GetText());
    }

    private void Repl()
    {

    }

    private void RunFile(string filePath)
    {
        string source = System.IO.File.ReadAllText(@filePath);
        RunSource(source);
    }

    private void RunSource(string source)
    {
        logger.ClearLog();

        //logger.LogPrint("Compiling source:\n\n{0}\n", source);
        source += "\n";

        vm = new VM(logger);

        InterpretResult result;
        if (source != "" && source != null)
        {
            result = vm.Interpret(source);
            logger.LogPrint("\nResult: {0}", result.ToString());
        }
        else
            logger.LogPrint("Could not read source.");
    }

    /* private void CreateTestChunk(Chunk chunk)
    {
        int constantIndex = chunk.AddConstant(1.2);
        chunk.Add(OpCode.OP_CONSTANT, 123);
        chunk.Add(constantIndex, 123);

        constantIndex = chunk.AddConstant(3.4);
        chunk.Add(OpCode.OP_CONSTANT, 123);
        chunk.Add(constantIndex, 123);

        chunk.Add(OpCode.OP_ADD, 123);

        constantIndex = chunk.AddConstant(5.6);
        chunk.Add(OpCode.OP_CONSTANT, 123);
        chunk.Add(constantIndex, 123);

        chunk.Add(OpCode.OP_DIVIDE, 123);
        chunk.Add(OpCode.OP_NEGATE, 123);
        chunk.Add(OpCode.OP_RETURN, 123);
    }

    private void DissasembleChunk(Chunk chunk)
    {
        disassembler.DisassembleChunk(chunk, "Test chunk");
    }

    private void InterpretChunk(Chunk chunk)
    {
        vm.Interpret(chunk);
    } */

}

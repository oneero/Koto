using Godot;
using System;
using System.Collections.Generic;
//using static Globals.Logger;

public enum InterpretResult
{
    OK,
    COMPILE_ERROR,
    RUNTIME_ERROR
}

public class VM
{
    private Chunk currentChunk = null;

    // Instruction pointer
    // Points to the next instruction about to be executed
    // In C this should be a pointer to the memory address of the bytecode array
    private byte ip;

    //private const int STACK_MAX = 256;
    private Stack<double> stack;

    private Compiler compiler;

    // This is for debugging. Perhaps wrap in #if (DEBUG) #endif ?
    private Disassembler disassembler;

    private Logger logger;

    public VM(Logger logger)
    {
        this.logger = logger;
        stack = new Stack<double>();
        compiler = new Compiler(logger);
        disassembler = new Disassembler(logger);
    }

    public InterpretResult Interpret(string source)
    {
        Chunk chunk = new Chunk();

        if (!compiler.Compile(source, ref chunk))
        {
            return InterpretResult.COMPILE_ERROR;
        }

        currentChunk = chunk;
        ip = 0;

        InterpretResult result = Run();

        return result;
    }

    private InterpretResult Run()
    {
        for (;;)
        {
            // Get next instruction
            OpCode instruction = (OpCode)ReadByte();

            // Decode / dispatch instruction
            switch(instruction)
            {
                case OpCode.CONSTANT:
                    double constant = ReadConstant();
                    stack.Push(constant);
                    break;

                case OpCode.ADD:
                    BinaryOp((a, b) => a + b);
                    break;

                case OpCode.SUBTRACT:
                    BinaryOp((a, b) => a - b);
                    break;

                case OpCode.MULTIPLY:
                    BinaryOp((a, b) => a * b);
                    break;

                case OpCode.DIVIDE:
                    BinaryOp((a, b) => a / b);
                    break;

                case OpCode.NEGATE:
                    stack.Push(-stack.Pop());
                    break;
                
                case OpCode.RETURN:
                    logger.LogPrint("\n=> {0}", stack.Pop());
                    return InterpretResult.OK;

                
            }
        }
    }

    // Might be the wrong place for ReadyByte and ReadConstant

    private byte ReadByte()
    {
        byte instruction = currentChunk.code[ip];
        ip++;
        return instruction;
    }

    private double ReadConstant()
    {
        int constantIndex =(int)ReadByte();
        return currentChunk.constants[constantIndex];
    }

    private void BinaryOp(Func<double, double, double> op)
    {
        double b = stack.Pop();
        double a = stack.Pop();
        stack.Push(op(a, b));
    }
}

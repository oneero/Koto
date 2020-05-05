using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Events;

public enum InterpretResult
{
    OK,
    COMPILE_ERROR,
    RUNTIME_ERROR,
    STEPPING
}

[System.Serializable]
public class InterpreterEvent : UnityEvent<InterpretResult> {}

public class VM
{
    private Chunk currentChunk = null;

    // Instruction pointer
    // Points to the next instruction about to be executed
    // In C this should be a pointer to the memory address of the bytecode array
    private byte ip;

    //private const int STACK_MAX = 256;
    // This was a Stack before, but we want index access for juggling..
    private List<Value> stack;

    private Compiler compiler;

    // This is for debugging. Perhaps wrap in #if (DEBUG) #endif ?
    private Disassembler disassembler;

    private Logger logger;

    private VMGC vmgc;
    
    private bool allowStep = false;

    public InterpreterEvent OnRunFinished;

    private InterpretResult runResult = InterpretResult.STEPPING;

    // Dictionaries/HashTables for variable
    private Dictionary<string, Value> globals;

    public VM(VMGC vmgc, Logger logger)
    {
        this.vmgc = vmgc;
        this.logger = logger;
        stack = new List<Value>();
        compiler = new Compiler(logger);
        disassembler = new Disassembler(logger);

        globals = new Dictionary<string, Value>();
        
        OnRunFinished = new InterpreterEvent();
    }

    public void Interpret(string source)
    {
        Chunk chunk = new Chunk();

        if (!compiler.Compile(source, ref chunk))
        {
            runResult = InterpretResult.COMPILE_ERROR;
            OnRunFinished?.Invoke(runResult);
            return;
        }

        currentChunk = chunk;
        ip = 0;

        Run(50); 
    }

    private InterpretResult Step()
    {
        //GD.Print("Stepping");

        // Get next instruction
        OpCode instruction = (OpCode)ReadByte();

        // Decode / dispatch instruction
        switch(instruction)
        {
            case OpCode.CONSTANT:
                Value constant = ReadConstant();
                stack.Push(constant);
                break;

            case OpCode.NIL: stack.Push(new Value()); break;
            case OpCode.TRUE: stack.Push(new Value(true)); break;
            case OpCode.FALSE: stack.Push(new Value(false)); break;

            case OpCode.POP: stack.Pop(); break;

            case OpCode.GET_LOCAL:
                byte getSlot = ReadByte();
                stack.Push(stack[getSlot]); // Stack juggling because we dont use registers
                break;

            case OpCode.SET_LOCAL:
                byte setSlot = ReadByte();
                stack[setSlot] = Peek();
                break;

            case OpCode.GET_GLOBAL:
                string stringToGet = ReadString();
                Value valueToGet;
                if (globals.TryGetValue(stringToGet, out valueToGet))
                {
                    stack.Push(valueToGet);
                }
                else
                {
                    RuntimeError("Undefined variable '{0}'", stringToGet);
                }
                break;

            case OpCode.DEFINE_GLOBAL:
                string stringToDefine = ReadString();
                globals[stringToDefine] = Peek();
                stack.Pop();
                break;

            case OpCode.SET_GLOBAL:
                string stringToSet = ReadString();
                if (globals.ContainsKey(stringToSet))
                {
                    globals[stringToSet] = Peek();
                }
                else
                {
                    RuntimeError("Undefined variable '{0}'", stringToSet);
                }
                break;

            case OpCode.EQUAL:
                EqualityOp();
                break;
                
            case OpCode.GREATER:
                if (!BinaryOp((a, b) => a > b))
                    return InterpretResult.RUNTIME_ERROR;
                break;

            case OpCode.LESS:
                if (!BinaryOp((a, b) => a < b))
                    return InterpretResult.RUNTIME_ERROR;
                break;

            case OpCode.ADD:
                if (Peek().IsString() && Peek(1).IsString())
                {
                    Concatenate();
                }
                else if (Peek().IsNumber() && Peek(1).IsNumber())
                {
                    double b = stack.Pop().AsNumber();
                    double a = stack.Pop().AsNumber();
                    stack.Push(new Value(a + b));
                }
                else
                {
                    return InterpretResult.RUNTIME_ERROR;
                }
                break;

            case OpCode.SUBTRACT:
                if (!BinaryOp((a, b) => a - b))
                    return InterpretResult.RUNTIME_ERROR;
                break;

            case OpCode.MULTIPLY:
                if (!BinaryOp((a, b) => a * b))
                    return InterpretResult.RUNTIME_ERROR;
                break;

            case OpCode.DIVIDE:
                if (!BinaryOp((a, b) => a / b))
                    return InterpretResult.RUNTIME_ERROR;
                break;

            case OpCode.NOT:
                stack.Push(new Value(IsFalsey(stack.Pop())));
                break;

            case OpCode.NEGATE:
                if (!Peek().IsNumber())
                {
                    RuntimeError("Operand must be a number.");
                    return InterpretResult.RUNTIME_ERROR;
                }
                stack.Push(new Value(-stack.Pop().AsNumber()));
                break;

            case OpCode.PRINT:
                logger.LogPrint(stack.Pop().ToString());
                break;
            
            case OpCode.JUMP:
                UInt16 jumpOffset = ReadShort();
                ip += (byte)jumpOffset;
                break;
            
            case OpCode.JUMP_IF_FALSE:
                UInt16 conditionalJumpOffset = ReadShort();
                if (IsFalsey(Peek(0))) ip += (byte)conditionalJumpOffset;
                break;
            
            case OpCode.LOOP:
                UInt16 loopOffset = ReadShort();
                ip -= (byte)loopOffset;
                break;
            
            case OpCode.RETURN:
                // Exit
                return InterpretResult.OK;

            // VMGC

            case OpCode.WAIT:
                vmgc.Wait();
                break;

            case OpCode.READ:
                
                break;

            case OpCode.WRITE:
                double data2 = stack.Pop().AsNumber();
                double data1 = stack.Pop().AsNumber();
                double portIndex  = stack.Pop().AsNumber();
                vmgc.SendData(portIndex, data1, data2);
                break;
        }

        return InterpretResult.STEPPING;
    }

    private async void Run(int interval)
    {
        InterpretResult r = InterpretResult.STEPPING;
        while ( r == InterpretResult.STEPPING )
        {
            await Task.Delay(interval);
            r = Step();
        }
        OnRunFinished?.Invoke(r);
    }

    private InterpretResult Run()
    {
        for (;;)
        {
            InterpretResult stepResult = Step();
            if (stepResult != InterpretResult.STEPPING)
                return stepResult;
        }
    }

    private byte ReadByte()
    {
        byte instruction = currentChunk.code[ip];
        ip++;
        return instruction;
    }

    private Value ReadConstant()
    {
        int constantIndex = (int)ReadByte();
        return currentChunk.constants[constantIndex];
    }

    private UInt16 ReadShort()
    {
        ip += 2;
        return (UInt16)(currentChunk.code[ip - 2] << 8 | currentChunk.code[ip - 1]);
    }

    private string ReadString()
    {
        return ReadConstant().AsString();
    }
    
    private void EqualityOp()
    {
        Value b = stack.Pop();
        Value a = stack.Pop();
        stack.Push(new Value(ValuesEqual(a, b)));
    }

    private bool BinaryOp<T>(Func<double, double, T> op)
    {
        if (!Peek().IsNumber() || !Peek(1).IsNumber())
        {
            RuntimeError("Operands must be numbers.");
            return false; //InterpretResult.RUNTIME_ERROR;
        }
        double b = stack.Pop().AsNumber();
        double a = stack.Pop().AsNumber();

        // Have to do runtime conversion to actual value from generic func return
        // This sucks. Look into making Value completely generic?
        var genericResult = op(a, b);
        Value valueResult = null;

        if (typeof(T) == typeof(bool))
            valueResult = new Value((bool)Convert.ChangeType(genericResult, typeof(bool)));
        else if (typeof(T) == typeof(double))
            valueResult = new Value((double)Convert.ChangeType(genericResult, typeof(double)));
        else
        {
            RuntimeError("Binary operation value conversion failed.");
            return false;
        }

        stack.Push(valueResult);
        return true;
    }

    private void Concatenate()
    {
        string bString = stack.Pop().AsString();
        string aString = stack.Pop().AsString();
        string concat = aString + bString;
        Value result = new Value(new Obj(concat, false));
        stack.Push(result);
    }

    private void RuntimeError(string msg, params object[] args)
    {
        string message = String.Format(msg, args);
        logger.LogPrint(message);
        int line = currentChunk.lines[ip];
        logger.LogPrint("[line {0}] in script.", line);
    }

    // Utility wrapper for peeking into the Stack using Linq
    // This is unnecessary since we switched to List
    private Value Peek(int depth = 0)
    {
        return depth == 0 ? stack.Peek() : stack.Skip(depth).First();
    }

    private bool IsFalsey(Value value)
    {
        return value.IsNil() || (value.IsBool() && !value.AsBool());
    }

    private bool ValuesEqual(Value a, Value b)
    {
        if (a.type != b.type) return false;

        switch (a.type)
        {
            case ValueType.BOOL:    return a.AsBool() == b.AsBool();
            case ValueType.NIL:     return true;
            case ValueType.NUMBER:  return a.AsNumber() == b.AsNumber();
            case ValueType.OBJ:
                // Only support string objects for now
                string aString = a.ToString();
                string bString = b.ToString();
                return a == b;

            default:
                return false; // Unreachable.
        }
    }
}

// Extend List to act like the Stack we had before..
public static class MyExtensions
{
    public static T Pop<T>(this List<T> list)
    {
        int index = list.Count - 1;
        if (index < 0) return default(T);
            
        T value = list[index];
        list.RemoveAt(index);
        return value;
    }

    public static void Push<T>(this List<T> list, T item)
    {
        list.Add(item);
    }

    public static T Peek<T>(this List<T> list)
    {
        int index = list.Count - 1;
        T value = list[index];
        return value;
    }
}
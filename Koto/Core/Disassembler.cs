using System;

public class Disassembler
{
    private Logger logger;

    public Disassembler(Logger logger)
    {
        this.logger = logger;
    }

    public void DisassembleChunk(Chunk chunk, string name)
    {
        logger.LogPrint("== {0} ==", name);

        for (int offset = 0; offset < chunk.Count;)
        {
            offset = DisassembleInstruction(chunk, offset);
        }
    }

    public int DisassembleInstruction(Chunk chunk, int offset)
    {
        logger.Log("{0,4:d} ", offset);

        if (offset > 0 && chunk.lines[offset] == chunk.lines[offset-1])
            logger.Log("   | ");
        else
            logger.Log("{0,4:d} ", chunk.lines[offset]);

        Byte instruction = chunk.code[offset];

        switch((OpCode)instruction)
        {
            case OpCode.CONSTANT:
                return ConstantInstruction("OP_CONSTANT", chunk, offset);
            
            case OpCode.NIL:
                return SimpleInstruction("OP_NIL", offset);

            case OpCode.TRUE:
                return SimpleInstruction("OP_TRUE", offset);

            case OpCode.FALSE:
                return SimpleInstruction("OP_FALSE", offset);

            case OpCode.POP:
                return SimpleInstruction("OP_POP", offset);

            case OpCode.GET_LOCAL:
                return ByteInstruction("OP_GET_LOCAL", chunk, offset);

            case OpCode.SET_LOCAL:
                return ByteInstruction("OP_SET_LOCAL", chunk, offset);

            case OpCode.GET_GLOBAL:
                return ConstantInstruction("OP_GET_GLOBAL", chunk, offset);

            case OpCode.DEFINE_GLOBAL:
                return ConstantInstruction("OP_DEFINE_GLOBAL", chunk, offset);

            case OpCode.SET_GLOBAL:
                return ConstantInstruction("OP_SET_GLOBAL", chunk, offset);

            case OpCode.EQUAL:
                return SimpleInstruction("OP_EQUAL", offset);

            case OpCode.GREATER:
                return SimpleInstruction("OP_GREATER", offset);

            case OpCode.LESS:
                return SimpleInstruction("OP_LESS", offset);

            case OpCode.ADD:
                return SimpleInstruction("OP_ADD", offset);

            case OpCode.SUBTRACT:
                return SimpleInstruction("OP_SUBTRACT", offset);

            case OpCode.MULTIPLY:
                return SimpleInstruction("OP_MULTIPLY", offset);

            case OpCode.DIVIDE:
                return SimpleInstruction("OP_DIVIDE", offset);

            case OpCode.NOT:
                return SimpleInstruction("OP_NOT", offset);

            case OpCode.NEGATE:
                return SimpleInstruction("OP_NEGATE", offset);

            case OpCode.PRINT:
                return SimpleInstruction("OP_PRINT", offset);
            
            case OpCode.JUMP:
                return JumpInstruction("OP_JUMP", 1, chunk, offset);
            
            case OpCode.JUMP_IF_FALSE:
                return JumpInstruction("OP_JUMP_IF_FALSE", 1, chunk, offset);
            
            case OpCode.LOOP:
                return JumpInstruction("OP_LOOP", -1, chunk, offset);

            case OpCode.RETURN:
                return SimpleInstruction("OP_RETURN", offset);

            case OpCode.WAIT:
                return SimpleInstruction("OP_WAIT", offset);

            case OpCode.READ:
                return SimpleInstruction("OP_READ", offset);

            case OpCode.WRITE:
                return SimpleInstruction("OP_WRITE", offset);

            default:
                logger.LogPrint("Unknown opcode: {0}", instruction);
                return offset + 1;   
        }
    }

    private int ConstantInstruction(string name, Chunk chunk, int offset)
    {
        // get constant index from next byte
        byte constantIndex = chunk.code[offset + 1];
        logger.Log("{0,-16:d} {1,4:d} ", name, constantIndex);
        logger.LogPrint("{0:f}", chunk.constants[constantIndex]);

        // skip over the constant index
        return offset + 2;
    }

    private int ByteInstruction(string name, Chunk chunk, int offset)
    {
        byte slot = chunk.code[offset + 1];
        logger.LogPrint("{0,-16:d} {1,4:d}", name, slot);
        return offset + 2;
    }

    private int SimpleInstruction(string name, int offset)
    {
        logger.LogPrint("{0}", name);

        return offset + 1;
    }

    private int JumpInstruction(string name, int sign, Chunk chunk, int offset)
    {
        UInt16 jump = (UInt16)(chunk.code[offset + 1] << 8);
        jump |= chunk.code[offset + 2];
        logger.LogPrint("{0,-16:d} {1,4:d} -> {2}", name, offset, offset + 3 + sign * jump);
        return offset + 3;
    }

    
}
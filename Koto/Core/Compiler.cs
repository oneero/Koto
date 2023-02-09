using System;
using System.Collections.Generic;

public class Compiler
{
    private int MAX_CONSTANT_IN_CHUNK = 256;

    private Scanner scanner;
    private Parser parser;
    private Chunk compilingChunk;
    private ParseRule[] rules;
    private Disassembler disassembler;

    // Locals
    Local[] locals;
    private int localCount;
    private int scopeDepth;

    private Logger logger;

    public Compiler(Logger logger)
    {
        this.logger = logger;

        parser = new Parser();
        disassembler = new Disassembler(logger);

        locals = new Local[MAX_CONSTANT_IN_CHUNK + 1]; // Could make this a list instead..?
        localCount = 0;
        scopeDepth = 0;

        rules = new ParseRule[] {
            new ParseRule(Grouping, null,    Precedence.NONE),       // TOKEN_LEFT_PAREN      
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_RIGHT_PAREN     
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_LEFT_BRACE
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_RIGHT_BRACE     
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_COMMA           
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_DOT             
            new ParseRule(Unary,    Binary,  Precedence.TERM),       // TOKEN_MINUS           
            new ParseRule(null,     Binary,  Precedence.TERM),       // TOKEN_PLUS            
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_SEMICOLON       
            new ParseRule(null,     Binary,  Precedence.FACTOR),     // TOKEN_SLASH           
            new ParseRule(null,     Binary,  Precedence.FACTOR),     // TOKEN_STAR            
            new ParseRule(Unary,    null,    Precedence.NONE),       // TOKEN_BANG            
            new ParseRule(null,     Binary,  Precedence.EQUALITY),   // TOKEN_BANG_EQUAL      
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_EQUAL           
            new ParseRule(null,     Binary,  Precedence.COMPARISON), // TOKEN_EQUAL_EQUAL     
            new ParseRule(null,     Binary,  Precedence.COMPARISON), // TOKEN_GREATER         
            new ParseRule(null,     Binary,  Precedence.COMPARISON), // TOKEN_GREATER_EQUAL   
            new ParseRule(null,     Binary,  Precedence.COMPARISON), // TOKEN_LESS            
            new ParseRule(null,     Binary,  Precedence.COMPARISON), // TOKEN_LESS_EQUAL      
            new ParseRule(Variable, null,    Precedence.NONE),       // TOKEN_IDENTIFIER      
            new ParseRule(String,   null,    Precedence.NONE),       // TOKEN_STRING          
            new ParseRule(Number,   null,    Precedence.NONE),       // TOKEN_NUMBER          
            new ParseRule(null,     And,     Precedence.AND),        // TOKEN_AND             
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_CLASS           
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_ELSE            
            new ParseRule(Literal,  null,    Precedence.NONE),       // TOKEN_FALSE           
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_FOR             
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_FUN             
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_IF              
            new ParseRule(Literal,  null,    Precedence.NONE),       // TOKEN_NIL             
            new ParseRule(null,     Or,      Precedence.OR),         // TOKEN_OR              
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_PRINT           
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_RETURN          
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_SUPER           
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_THIS            
            new ParseRule(Literal,  null,    Precedence.NONE),       // TOKEN_TRUE            
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_VAR             
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_WHILE           
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_ERROR           
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_EOF

            new ParseRule(null,     null,   Precedence.NONE),        // TOKEN_WAIT
            new ParseRule(null,     null,   Precedence.NONE),        // TOKEN_READ
            new ParseRule(null,     null,   Precedence.NONE)         // TOKEN_WRITE
        };
    }

    public bool Compile(string source, ref Chunk chunk)
    {
        scanner = new Scanner(source);

        compilingChunk = chunk;

        Advance();

        while (!Match(TokenType.EOF))
        {
            Declaration();
        } 

        EndCompiler();
        return !parser.hadError;
    }

    private void Advance()
    {
        parser.previous = parser.current;

        for (;;)
        {
            parser.current = scanner.ScanToken();
            //GD.Print("T => ", parser.current.type.ToString());

            if (parser.current.type != TokenType.ERROR)
                break;

            ErrorAtCurrent(parser.current.content);
        }
    }

    private void Declaration()
    {
        if (Match(TokenType.VAR))
        {
            VarDeclaration();
        }
        else
        {
            Statement();
        }

        if (parser.panicMode) Synchronize();
    }

    private void VarDeclaration()
    {
        byte global = ParseVariable("Expected variable name.");

        if (Match(TokenType.EQUAL))
        {
            Expression();
        }
        else
        {
            EmitByte(OpCode.NIL);
        }

        Consume(TokenType.SEMICOLON, "Expected ';' after variable declaration.");

        DefineVariable(global);
    }

    private byte ParseVariable(string errorMessage)
    {
        Consume(TokenType.IDENTIFIER, errorMessage);

        DeclareVariable();
        if (scopeDepth > 0) return 0;

        return IdentifierConstant(ref parser.previous);
    }

    private byte IdentifierConstant(ref Token token)
    {
        return MakeConstant(new Value(new Obj(token)));
    }

    private void DeclareVariable()
    {
        if (scopeDepth == 0) return;    // Globals are late bound, bail out!
        Token name = parser.previous;
        
        // Lets check that the variable is not already declared in this scope
        for (int i = localCount -1; i >= 0; i--)
        {
            Local local = locals[i];
            if (local.depth != -1 && local.depth < scopeDepth)
            {
                break;
            }

            if (IdentifiersEqual(name, local.name))
            {
                Error("Variable with this name already declared in this scope.");
            }
        }

        AddLocal(name);
    }

    private bool IdentifiersEqual(Token a, Token b)
    {
        if (a.length != b.length) return false; // Quick fail
        return a.content == b.content; // Should this be a.content == b.content ?
    }

    private int ResolveLocal(Token name)
    {
        for (int i = localCount - 1; i >= 0; i--)
        {
            Local local = locals[i];
            if (IdentifiersEqual(name, local.name))
            {
                if (local.depth == -1)
                {
                    Error("Cannot read local variable in its own initializer.");
                }
                return i;
            }
                
        }
        return -1;
    }

    private void AddLocal(Token name)
    {
        if (localCount == MAX_CONSTANT_IN_CHUNK)
        {
            Error("Too many local variables in function.");
            return;
        }
        Local local = new Local();
        locals[localCount++] = local;
        local.name = name;
        local.depth = -1; // -1 means uninitialized
    }

    private void And(bool canAssingn)
    {
        int endJump = EmitJump(OpCode.JUMP_IF_FALSE);
        
        EmitByte(OpCode.POP);
        ParsePrecedence(Precedence.AND);
        
        PatchJump(endJump);
    }

    private void Or(bool canAssign)
    {
        int elseJump = EmitJump(OpCode.JUMP_IF_FALSE);
        int endJump = EmitJump(OpCode.JUMP);
        
        PatchJump(elseJump);
        EmitByte(OpCode.POP);
        
        ParsePrecedence(Precedence.OR);
        PatchJump(endJump);
    }

    private void DefineVariable(byte global)
    {
        logger.LogPrint("Defining variable, scopeDepth = {0}", scopeDepth);
        if (scopeDepth > 0)
        {
            MarkInitialized();
            return;
        }
        EmitBytes(OpCode.DEFINE_GLOBAL, global);
    }

    private void MarkInitialized()
    {
        if (scopeDepth == 0) return;
        locals[localCount - 1].depth = scopeDepth;
    }

    private void Variable(bool canAssign)
    {
        NamedVariable(parser.previous, canAssign);
    }

    private void NamedVariable(Token token, bool canAssign)
    {
        OpCode getOp, setOp;
        int arg = ResolveLocal(token); // Compiler as argument?

        if (arg != -1)
        {
            getOp = OpCode.GET_LOCAL;
            setOp = OpCode.SET_LOCAL;
        }
        else
        {
            arg = IdentifierConstant(ref token);
            getOp = OpCode.GET_GLOBAL;
            setOp = OpCode.SET_GLOBAL;
        }

        if (canAssign && Match(TokenType.EQUAL))
        {
            Expression();
            EmitBytes(setOp, (byte)arg);
        }
        else
        {
            EmitBytes(getOp, (byte)arg);
        }
    }

    private void Statement()
    {
        if (Match(TokenType.PRINT))
        {
            SingleArgumentStatement(OpCode.PRINT);
        }
        else if (Match(TokenType.FOR))
        {
            ForStatement();
        }
        else if (Match(TokenType.IF))
        {
            IfStatement();
        }
        else if (Match(TokenType.WHILE))
        {
            WhileStatement();
        }
        else if (Match(TokenType.LEFT_BRACE))
        {
            BeginScope();
            Block();
            EndScope();
        }
        else if (Match(TokenType.WRITE)) WriteStatement();
        else
        {
            ExpressionStatement();
        }
    }

    /* private void PrintStatement()
    {
        Expression();
        Consume(TokenType.SEMICOLON, "Expected ';' after value.");
        EmitByte(OpCode.PRINT);
    } */

    private void SingleArgumentStatement(OpCode OpToEmit)
    {
        Expression();
        Consume(TokenType.SEMICOLON, "Expected ';' after value.");
        EmitByte(OpToEmit);
    }

    // Initializer clause, condition clause, increment clause
    private void ForStatement()
    {
        // Initializer could declare a variable, which means we need to scope it.
        BeginScope();
        
        Consume(TokenType.LEFT_PAREN, "Expected '(' after 'for'.");
        
        // Initializer clause
        if (Match(TokenType.SEMICOLON))
        {
            // No initializer
        } else if (Match(TokenType.VAR))
        {
           VarDeclaration(); 
        }
        else
        {
            ExpressionStatement();
        }

        int loopStart = CurrentChunk().Count;
        
        // Condition clause
        int exitJump = -1;
        
        // Check if the clause exists since it's optional
        if (!Match(TokenType.SEMICOLON))
        {
            Expression();
            Consume(TokenType.SEMICOLON, "Expected ';' after loop condition.");
            
            // Jump out if condition is falsey
            exitJump = EmitJump(OpCode.JUMP_IF_FALSE);
            EmitByte(OpCode.POP);
        }
        
        // Increment clause
        if (!Match(TokenType.RIGHT_PAREN))
        {
            int bodyJump = EmitJump(OpCode.JUMP);
            int incrementStart = CurrentChunk().Count;
            
            Expression();
            EmitByte(OpCode.POP);
            Consume(TokenType.RIGHT_PAREN, "Expected ')' after for clauses.");
            
            EmitLoop(loopStart);
            loopStart = incrementStart;
            PatchJump(bodyJump);
        }
        
        Statement();
        
        EmitLoop(loopStart);

        if (exitJump != -1)
        {
            PatchJump(exitJump);
            EmitByte(OpCode.POP);
        }
        
        EndScope();
    }

    private void IfStatement()
    {
        Consume(TokenType.LEFT_PAREN, "Expected '(' after 'if'.");
        Expression();
        Consume(TokenType.RIGHT_PAREN, "Expected ')' after condition.");

        // Emit jump instruction with placeholder offset
        int thenJump = EmitJump(OpCode.JUMP_IF_FALSE);
        
        // Clean up condition value left in stack here or..
        EmitByte(OpCode.POP);
        
        // Compile the then-body
        Statement();

        // We need to skip the else block at the end of the then block
        int elseJump = EmitJump(OpCode.JUMP);

        // Backpatch the thenJump instruction with correct offset
        PatchJump(thenJump);
        
        // ..here.
        EmitByte(OpCode.POP);
        
        if (Match(TokenType.ELSE)) Statement();
        
        // Backpatch the elseJump instruction
        PatchJump(elseJump);
    }

    private void WhileStatement()
    {
        // Capture the point to loop back to
        int loopStart = CurrentChunk().Count;
        
        // Check condition
        Consume(TokenType.LEFT_PAREN, "Expected '(' after 'while'.");
        Expression();
        Consume(TokenType.RIGHT_PAREN, "Expected ')' after condition.");

        // Prepare jump if the condition is falsey
        int exitJump = EmitJump(OpCode.JUMP_IF_FALSE);
        
        EmitByte(OpCode.POP);
        Statement();

        EmitLoop(loopStart);
        
        PatchJump(exitJump);
        EmitByte(OpCode.POP);
    }

    private void BeginScope()
    {
        //logger.Log("BeginScope: {0}", scopeDepth);
        scopeDepth++;
        //logger.LogPrint(" -> {0}", scopeDepth);
    }

    private void EndScope()
    {
        //logger.Log("EndScope: {0}", scopeDepth);
        scopeDepth--;
        //logger.LogPrint(" -> {0}", scopeDepth);

        // Clean up locals of the ended scope
        while (localCount > 0 &&
               locals[localCount - 1].depth > scopeDepth)
        {
            EmitByte(OpCode.POP); // This could be optimized with a pop-n operation
            localCount--;   
        }
    }

    private void Block()
    {
        while (!Check(TokenType.RIGHT_BRACE) && !Check(TokenType.EOF))
        {
            Declaration();
        }
        Consume(TokenType.RIGHT_BRACE, "Expected '}' after block.");
    }

    private void WriteStatement()
    {
        Expression(); // Port index
        Expression(); // First argument
        Expression(); // Second argument (these should be combined to some new type?)
        Consume(TokenType.SEMICOLON, "Expected ';' after values.");
        EmitByte(OpCode.WRITE);
    }

    private void ExpressionStatement()
    {
        Expression();
        Consume(TokenType.SEMICOLON, "Expected ';' after expression.");
        EmitByte(OpCode.POP);
    }

    private void Expression()
    {
        ParsePrecedence(Precedence.ASSIGNMENT);
    }

    private void Number(bool canAssign)
    {
        double value = Convert.ToDouble(parser.previous.content);
        EmitConstant(new Value(value));
    }
    
    private void String(bool canAssign)
    {
        EmitConstant(new Value(new Obj(parser.previous.content)));
    }

    private void Grouping(bool canAssign)
    {
        Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
    }

    private void Unary(bool canAssign)
    {
        TokenType operatorType = parser.previous.type;

        // Compile operand
        ParsePrecedence(Precedence.UNARY);

        // Emit operator instruction
        switch (operatorType)
        {
            case TokenType.BANG: EmitByte(OpCode.NOT); break;
            case TokenType.MINUS: EmitByte(OpCode.NEGATE); break;
            default:
                return; // Unreachable.
        }
    }

    private void Binary(bool canAssign)
    {
        // Remember the operator.
        TokenType operatorType = parser.previous.type;
        
        // Compile the right operand.
        ParseRule rule = GetRule(operatorType);
        ParsePrecedence((Precedence)(rule.precedence + 1)); // hehehee

        // Emit the operator instruction.
        switch (operatorType)
        {
            case TokenType.BANG_EQUAL: EmitBytes(OpCode.EQUAL, OpCode.NOT); break;
            case TokenType.EQUAL_EQUAL: EmitByte(OpCode.EQUAL); break;
            case TokenType.GREATER: EmitByte(OpCode.GREATER); break;
            case TokenType.GREATER_EQUAL: EmitBytes(OpCode.LESS, OpCode.NOT); break; // >= is the same as !(<)
            case TokenType.LESS: EmitByte(OpCode.LESS); break;
            case TokenType.LESS_EQUAL: EmitBytes(OpCode.GREATER, OpCode.NOT); break; // <= is the same as !(>)
            case TokenType.PLUS : EmitByte(OpCode.ADD); break;
            case TokenType.MINUS: EmitByte(OpCode.SUBTRACT); break;
            case TokenType.STAR : EmitByte(OpCode.MULTIPLY); break;
            case TokenType.SLASH: EmitByte(OpCode.DIVIDE); break;
            default:
                return; // Unreachable.
        }
    }

    private void Literal(bool canAssign)
    {
        switch (parser.previous.type)
        {
            case TokenType.FALSE: EmitByte(OpCode.FALSE); break;
            case TokenType.TRUE: EmitByte(OpCode.TRUE); break;
            case TokenType.NIL: EmitByte(OpCode.NIL); break;
            default:
                return; // Unreachable.
        }
    }

    private ParseRule GetRule(TokenType token)
    {
        //logger.LogPrint("GetRule: {0}({1})", token, (int)token);
        return rules[(int)token];
    }

    // Functions for adding new operations to the bytecode
    private void EmitByte(OpCode newOp) { EmitByte((byte)newOp); }
    private void EmitByte(byte newByte)
    {
        CurrentChunk().Add(newByte, parser.previous.line);
    }

    private void EmitBytes(OpCode newOp1, byte newOp2) { EmitBytes((byte) newOp1, (byte)newOp2); }
    private void EmitBytes(OpCode newOp1, OpCode newOp2) { EmitBytes((byte) newOp1, (byte)newOp2); }
    private void EmitBytes(byte newByte1, byte newByte2)
    {
        EmitByte(newByte1);
        EmitByte(newByte2);
    }

    // Helper function for loops
    private void EmitLoop(int loopStart)
    {
        EmitByte(OpCode.LOOP);

        int offset = CurrentChunk().Count - loopStart + 2;
        if (offset > UInt16.MaxValue) Error("Loop body too large.");
        
        EmitByte((byte)((offset >> 8) & 0xff));
        EmitByte((byte)(offset & 0xff));
    }

    // Helper function for if-statements
    // Emits instruction and a placeholder 16-bit offset
    // Return instruction position for backpatching
    private int EmitJump(OpCode newOp) { return EmitJump((byte) newOp); }
    private int EmitJump(byte instruction)
    {
        EmitByte(instruction);
        EmitByte(0xff);
        EmitByte(0xff);
        return CurrentChunk().Count - 2;
    }

    // For backpatching EmitJump
    // Goes back to the jump instruction and replaces placeholder offset with the calculated one
    private void PatchJump(int offset)
    {
        // -2 to adjust for the jump offset itself
        int jump = CurrentChunk().Count - offset - 2;
        
        if (jump > UInt16.MaxValue) Error("Too much code to jump over.");

        CurrentChunk().code[offset] = (byte)((jump >> 8) & 0xff);
        CurrentChunk().code[offset + 1] = (byte)(jump & 0xff);
    }

    private void EmitReturn()
    {
        EmitByte(OpCode.RETURN);
    }

    private void EmitConstant(Value value)
    {
        EmitBytes(OpCode.CONSTANT, MakeConstant(value));
    }

    private byte MakeConstant(Value value)
    {
        int constantIndex = CurrentChunk().AddConstant(value);
        if (constantIndex > MAX_CONSTANT_IN_CHUNK)
        {
            Error("Too many constants in one chunk.");
            return 0;
        }
        return (byte)constantIndex;
    }

    private Chunk CurrentChunk()
    {
        return compilingChunk;
    }

    private void Consume(TokenType type, string message)
    {
        if (parser.current.type == type)
        {
            Advance();
            return;
        }

        ErrorAtCurrent(message);
    }

    // Check if current token matches what we want
    // If it does, consume and return true
    // If not, leave it alone and return false
    private bool Match(TokenType type)
    {
        if (!Check(type)) return false;
        Advance();
        return true;
    }

    private bool MatchAny(List<TokenType> listOfTypes, out TokenType matchedType)
    {
        foreach (TokenType type in listOfTypes)
        {
            if (Match(type))
            {
                matchedType = type;
                return true;
            }
        }
        matchedType = TokenType.NIL;
        return false;
    }

    // Helper for Match()
    private bool Check(TokenType type)
    {
        return parser.current.type == type;
    }


    private void ParsePrecedence(Precedence precedence)
    {
        Advance();
        Action<bool> prefixRule = GetRule(parser.previous.type).prefixFunction;
        if (prefixRule == null)
        {
            Error("Expected expression.");
            return;
        }

        bool canAssign = precedence <= Precedence.ASSIGNMENT;
        prefixRule(canAssign);

        while (precedence <= GetRule(parser.current.type).precedence)
        {
            Advance();
            Action<bool> infixRule = GetRule(parser.previous.type).infixFunction;
            infixRule(canAssign);
        }

        if (canAssign && Match(TokenType.EQUAL))
        {
            Error("Invalid assignment target.");
            Expression();
        }
    }

    private void ErrorAtCurrent(string message)
    {
        ErrorAt(parser.current, message);
    }

    private void Error(string message)
    {
        ErrorAt(parser.previous, message);
    }

    private void ErrorAt(Token token, string message)
    {
        if (parser.panicMode) return;
        parser.panicMode = true;

        logger.Log("[{0}] Error", token.line);

        if (token.type == TokenType.EOF)
            logger.Log(" at end");
        else if (token.type == TokenType.ERROR)
        {
            // Nothing
        }
        else
            logger.Log(" at {0,6}", token.start);
        
        logger.LogPrint(": {0}", message);

        parser.hadError = true;
    }

    private void Synchronize()
    {
        parser.panicMode = false;

        while (parser.current.type != TokenType.EOF)
        {
            if (parser.previous.type == TokenType.SEMICOLON) return;

            switch (parser.current.type)
            {
                case TokenType.CLASS:
                case TokenType.FUN:                                   
                case TokenType.VAR:                                   
                case TokenType.FOR:                                   
                case TokenType.IF:                                    
                case TokenType.WHILE:                                 
                case TokenType.PRINT:                                 
                case TokenType.RETURN:
                case TokenType.WAIT:
                case TokenType.READ:
                case TokenType.WRITE:                                
                    return;
                
                default:
                    break; // Do nothing
            }

            Advance();
        }
    }

    private void EndCompiler()
    {
        EmitReturn();
#if DEBUG
        if (!parser.hadError)
            disassembler.DisassembleChunk(CurrentChunk(), "Bytecode disassembly");
#endif
    }

}

public class Parser
{
    public Token previous;
    public Token current;
    public bool hadError = false;
    public bool panicMode = false;
}

public class ParseRule
{
    public Action<bool> prefixFunction;
    public Action<bool> infixFunction;
    public Precedence precedence;

    public ParseRule(Action<bool> prefix, Action<bool> infix, Precedence precedence)
    {
        this.prefixFunction = prefix;
        this.infixFunction = infix;
        this.precedence = precedence;
    }
}

public enum Precedence
{
    NONE,                    
    ASSIGNMENT,  // =        
    OR,          // or       
    AND,         // and      
    EQUALITY,    // == !=    
    COMPARISON,  // < > <= >=
    TERM,        // + -      
    FACTOR,      // * /      
    UNARY,       // ! -      
    CALL,        // . () []  
    PRIMARY
}
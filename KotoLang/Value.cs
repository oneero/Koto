using Godot;
using System;

// For now I'll just make this a horrible memory-hog piece of shit and refactor later
// Might have to go with this in the future..
// https://stackoverflow.com/questions/3151702/discriminated-union-in-c-sharp

public class Value
{
    private ValueType type;
    private bool boolean;
    private double number;

    public Value(bool boolean)
    {
        this.type = ValueType.BOOL;
        this.boolean = boolean;
    }

    public Value(double number)
    {
        this.type = ValueType.NUMBER;
        this.number = number;
    }

    public bool AsBool()
    {
        if (IsBool())
            return boolean;
        else
            throw new Exception("Value conversion error.");
    }

    public double AsNumber()
    {
        if (IsNumber())
            return number;
        else
            throw new Exception("Value conversion error.");
    }

    public bool IsBool()
    {
        return type == ValueType.BOOL ? true : false;
    }

    public bool IsNumber()
    {
        return type == ValueType.NUMBER ? true : false;
    }

    public bool IsNil()
    {
        return type == ValueType.NIL ? true : false;
    }

    public override string ToString()
    {
        if (IsBool())
            return boolean.ToString();
        else if (IsNumber())
            return number.ToString();
        else
            return "NIL";
    }

}

public enum ValueType
{
    BOOL,
    NIL,
    NUMBER
};

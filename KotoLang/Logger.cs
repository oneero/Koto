using System;
using Godot;

public class Logger : Node
{
    [Export]
    public NodePath labelPath;
    private Label label;

    private static string log = "";
    private static string logLine = "";

    public Logger()
    {
        GD.Print("Logger()");
    }

    public override void _Ready()
    {
        GD.Print("Logger ready\n label path = ", labelPath.ToString());
        label = GetNode(labelPath) as Label;
        GD.Print("Label = ", label.ToString());
    }

    // Add to logLine
    public void Log(string msg, params object[] args)
    {
        string formatted = String.Format(msg, args);
        logLine += formatted;
    }

    // Print and clear logLine
    public void PrintLog()
    {
        GD.Print(logLine);
        log += logLine + "\n";
        UpdateLabel();
        logLine = "";
    }

    public void ClearLog()
    {
        log = "";
        logLine = "";
        UpdateLabel();
    }

    // Add to logLine and print, then clear
    public void LogPrint(string msg, params object[] args)
    {
        Log(msg, args);
        PrintLog();
    }

    public void UpdateLabel()
    {
        if (label != null)
            label.SetText(log);
        //EmitSignal(nameof(LogUpdated), log);
    }
}
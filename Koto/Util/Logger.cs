using System;
using TMPro;
using UnityEngine;

public class Logger : MonoBehaviour
{
    [SerializeField] private TMP_InputField logOutput;

    private static string log = "";
    private static string logLine = "";

    // Add to logLine
    public void Log(string msg, params object[] args)
    {
        string formatted = String.Format(msg, args);
        logLine += formatted;
    }

    // Print and clear logLine
    public void PrintLog()
    {
        //GD.Print(logLine);
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
        logOutput.text = log;
    }
}
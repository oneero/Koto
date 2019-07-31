using System;
using Godot;

namespace Globals
{
    class Logger : Node
    {
        static Logger instance;
        public static Logger Instance { get { return instance; } }

        [Signal]
        public delegate void LogUpdated();

        private static string log = "";

        Logger()
        {
            instance = this;
            LogPrint("Logger AutoLoaded.");
        }

        // Add to log
        public static void Log(string msg, params object[] args)
        {
            string formatted = String.Format(msg, args);
            log += formatted;
        }

        // Print and clear log
        public static void PrintLog()
        {
            GD.Print(log);
            UpdateLabel();
            log = "";
        }

        // Add to log and print, then clear
        public static void LogPrint(string msg, params object[] args)
        {
            Log(msg, args);
            PrintLog();
        }

        public static void UpdateLabel()
        {
            instance.EmitSignal(nameof(LogUpdated), log);
        }
    }
}
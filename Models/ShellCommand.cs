using BatRun;
using BatRun.Core;
using BatRun.UI;
using BatRun.Input;
using BatRun.Models;
using BatRun.Utils;

namespace BatRun.Models
{
    public enum CommandType
    {
        Application,
        Command,
        ScrapedGame
    }

    public class ShellCommand
    {
        public bool IsEnabled { get; set; }
        public string Path { get; set; } = string.Empty;
        public int DelaySeconds { get; set; }
        public int Order { get; set; }
        public CommandType Type { get; set; } = CommandType.Application;
        public string? GameSystemName { get; set; } 
        public string? GamePath { get; set; } 
        public bool AutoHide { get; set; }
        public bool DoubleLaunch { get; set; }
        public int DoubleLaunchDelay { get; set; }
        public bool OnlyOneInstance { get; set; }
        public bool LaunchRetroBatAtEnd { get; set; }
        public int RetroBatDelay { get; set; }
    }
}
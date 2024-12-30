public class ShellCommand
{
    public bool IsEnabled { get; set; }
    public string Path { get; set; } = string.Empty;
    public int DelaySeconds { get; set; }
    public int Order { get; set; }
    public bool IsCommand { get; set; }
    public bool AutoHide { get; set; }
    public bool DoubleLaunch { get; set; }
    public int DoubleLaunchDelay { get; set; }  // Délai en secondes entre les deux lancements
    public bool OnlyOneInstance { get; set; }  // Empêcher le lancement si déjà en cours d'exécution
    public bool LaunchRetroBatAtEnd { get; set; }
    public int RetroBatDelay { get; set; }
} 
/// Transient cross-scene game-flow state. The main menu / experiment-select writes the
/// chosen experiment here, then loads the lab scene where ExperimentLauncher reads it.
/// Kept deliberately tiny; persistent progress lives in ProgressionService/save file.
public static class GameFlow
{
    /// The experiment the player picked to run next (defaults to the tutorial).
    public static string SelectedModuleId = "tutorial-methane";

    public static void Select(string moduleId)
    {
        if (!string.IsNullOrEmpty(moduleId)) SelectedModuleId = moduleId;
    }
}

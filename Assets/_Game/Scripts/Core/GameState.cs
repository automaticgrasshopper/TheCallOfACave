namespace TCC.Core
{
    /// <summary>High level flow state for the whole game.</summary>
    public enum GameState
    {
        Boot,
        Playing,
        Paused,
        GameOver,
        Won
    }

    /// <summary>Supported languages. Chinese is the default; English is a placeholder
    /// so the localization pipeline is exercised from day one.</summary>
    public enum Language
    {
        ChineseSimplified = 0,
        English = 1
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Twain.UI.BotSettings;

/// <summary>
/// Specifies the system action performed after automatic processing completes.
/// </summary>
public enum BotShutdownAction
{
    Shutdown,
    Standby,
    Restart,
    Hibernate
}

/// <summary>
/// Provides presentation state for Twain bot-mode settings.
/// </summary>
public sealed partial class BotSettingsViewModel : ObservableObject
{
    /// <summary>
    /// Optional callback for resetting the active processing counters.
    /// </summary>
    public Action? ResetLiveNudgeCounters { get; set; }

    /// <summary>
    /// Gets or sets whether automatic article saving is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _autoSaveEnabled;

    /// <summary>
    /// Gets or sets the delay, in seconds, between automatic saves.
    /// </summary>
    [ObservableProperty]
    private int _autoSaveDelay;

    /// <summary>
    /// Gets or sets the maximum number of edits before processing stops.
    /// Zero indicates no limit.
    /// </summary>
    [ObservableProperty]
    private int _maximumEdits;

    /// <summary>
    /// Gets or sets whether the automatic Twain edit-summary suffix is suppressed.
    /// </summary>
    [ObservableProperty]
    private bool _suppressUsingTwain;

    /// <summary>
    /// Gets or sets whether the nudge timer is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _nudgeEnabled;

    /// <summary>
    /// Gets or sets whether an article is skipped after repeated nudge failures.
    /// </summary>
    [ObservableProperty]
    private bool _skipAfterRepeatedNudge;

    /// <summary>
    /// Gets the number of nudges recorded during the current session.
    /// </summary>
    [ObservableProperty]
    private int _nudgeCount;

    /// <summary>
    /// Gets or sets whether an automatic system action is performed when
    /// processing completes.
    /// </summary>
    [ObservableProperty]
    private bool _automaticShutdownEnabled;

    /// <summary>
    /// Gets or sets the system action performed when automatic processing
    /// completes.
    /// </summary>
    [ObservableProperty]
    private BotShutdownAction _shutdownAction = BotShutdownAction.Shutdown;

    /// <summary>
    /// Resets the active session nudge counters when available.
    /// </summary>
    [RelayCommand]
    private void ResetNudgeCount()
    {
        if (ResetLiveNudgeCounters is not null)
        {
            ResetLiveNudgeCounters();
            return;
        }

        NudgeCount = 0;
        CurrentArticleNudgeCount = 0;
    }

    /// <summary>
    /// Gets or sets the number of nudges attempted for the current article.
    /// </summary>
    [ObservableProperty]
    private int _currentArticleNudgeCount;

    /// <summary>
    /// Gets or sets whether the current account is permitted to use bot mode.
    /// </summary>
    [ObservableProperty]
    private bool _canUseBotMode;
}
using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Twain.UI.Shutdown;

/// <summary>
/// Displays a countdown before a requested shutdown-related action occurs.
/// </summary>
public partial class ShutdownNotificationWindow :
    Avalonia.Controls.Window
{
    private const int InitialCountdownSeconds = 120;

    private readonly DispatcherTimer _countdownTimer;

    private int _counter = InitialCountdownSeconds;
    private string _shutdownType = string.Empty;

    private string _promptTemplate =
        "AutoWikiBrowser has finished processing. The requested action is {0}.";

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ShutdownNotificationWindow"/> window.
    /// </summary>
    public ShutdownNotificationWindow()
    {
        InitializeComponent();

        _countdownTimer =
            new DispatcherTimer
            {
                Interval =
                    TimeSpan.FromSeconds(1)
            };

        _countdownTimer.Tick +=
            CountdownTimer_Tick;

        _countdownTimer.Start();

        UpdatePrompt();
        SetShutdownLabel(_counter);
    }

    /// <summary>
    /// Sets the shutdown action displayed by the notification window.
    /// </summary>
    public string ShutdownType
    {
        set
        {
            _shutdownType =
                value ?? string.Empty;

            UpdatePrompt();
            SetShutdownLabel(_counter);
        }
    }

    /// <summary>
    /// Gets or sets the template used for the shutdown prompt.
    /// </summary>
    /// <remarks>
    /// The template should contain a <c>{0}</c> placeholder for the
    /// shutdown action.
    /// </remarks>
    public string PromptTemplate
    {
        get => _promptTemplate;

        set
        {
            _promptTemplate =
                value ?? string.Empty;

            UpdatePrompt();
        }
    }

    /// <summary>
    /// Updates the main shutdown prompt.
    /// </summary>
    private void UpdatePrompt()
    {
        PromptText.Text =
            string.Format(
                _promptTemplate,
                _shutdownType);
    }

    /// <summary>
    /// Updates the countdown label with the selected shutdown action and
    /// remaining time.
    /// </summary>
    /// <param name="time">
    /// The number of seconds remaining.
    /// </param>
    private void SetShutdownLabel(
        int time)
    {
        TimerText.Text =
            $"Time until {_shutdownType}: {time}";
    }

    /// <summary>
    /// Decrements the shutdown countdown and closes the window when it
    /// expires.
    /// </summary>
    private void CountdownTimer_Tick(
        object? sender,
        EventArgs e)
    {
        _counter--;

        if (_counter <= 0)
        {
            _countdownTimer.Stop();
            Close(true);
            return;
        }

        SetShutdownLabel(_counter);
    }

    /// <summary>
    /// Confirms the requested shutdown action.
    /// </summary>
    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _countdownTimer.Stop();
        Close(true);
    }

    /// <summary>
    /// Cancels the requested shutdown action.
    /// </summary>
    private void CancelButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        _countdownTimer.Stop();
        Close(false);
    }
}
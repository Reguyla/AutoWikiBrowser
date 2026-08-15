namespace Twain.Diagnostics;

/// <summary>
/// Identifies the type of diagnostic information recorded by Twain.
/// </summary>
/// <remarks>
/// Categories allow diagnostic collection, storage, and reporting to be
/// controlled independently. Users may eventually enable or disable individual
/// categories according to their diagnostic and privacy preferences.
/// </remarks>
public enum DiagnosticCategory
{
    /// <summary>
    /// Information about an application session, such as session duration,
    /// pages processed, and edits completed.
    /// </summary>
    Session,

    /// <summary>
    /// Performance measurements, such as operation duration, throughput,
    /// and API response times.
    /// </summary>
    Performance,

    /// <summary>
    /// Information about exceptions encountered while the application is running.
    /// </summary>
    Exception,

    /// <summary>
    /// Information about the use of application features, modules, or plugins.
    /// </summary>
    FeatureUsage,

    /// <summary>
    /// Information about the application and runtime environment, such as
    /// Twain version, operating system, and .NET runtime.
    /// </summary>
    System
}
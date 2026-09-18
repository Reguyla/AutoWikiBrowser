using System.Threading;

namespace Twain.Core.DBScanner;

/// <summary>
/// Contains the user-configurable options for a database scan.
/// </summary>
public sealed class DatabaseScannerOptions
{
    // Dump
    public string FileName { get; set; } = string.Empty;

    public string StartFrom { get; set; } = string.Empty;

    // General
    public ThreadPriority Priority { get; set; } = ThreadPriority.Normal;

    public int ResultLimit { get; set; } = 30000;

    public bool IgnoreRedirects { get; set; } = true;

    public bool IgnoreComments { get; set; }

    // Namespaces
    public List<int> Namespaces { get; set; } = [];

    // Title
    public bool TitleContainsEnabled { get; set; }

    public string TitleContains { get; set; } = string.Empty;

    public bool TitleDoesNotContainEnabled { get; set; }

    public string TitleDoesNotContain { get; set; } = string.Empty;

    public bool TitleRegex { get; set; }

    public bool TitleCaseSensitive { get; set; }

    // Article text
    public bool ArticleContainsEnabled { get; set; }

    public string ArticleContains { get; set; } = string.Empty;

    public bool ArticleDoesNotContainEnabled { get; set; }

    public string ArticleDoesNotContain { get; set; } = string.Empty;

    public bool ArticleRegex { get; set; }

    public bool ArticleCaseSensitive { get; set; }

    public bool ArticleRegexMultiline { get; set; }

    public bool ArticleRegexSingleline { get; set; }

    // Revision date
    public bool SearchDates { get; set; }

    public DateTime DateFrom { get; set; }

    public DateTime DateTo { get; set; }

    // Protection
    public bool CheckProtection { get; set; }

    public string EditProtectionLevel { get; set; } = string.Empty;

    public string MoveProtectionLevel { get; set; } = string.Empty;

    // Article properties
    public int LengthComparison { get; set; }

    public int Length { get; set; } = 1000;

    public int LinkComparison { get; set; }

    public int Links { get; set; } = 5;

    public int WordComparison { get; set; }

    public int Words { get; set; } = 200;

    // AWB-specific checks
    public bool CheckBadLinks { get; set; }

    public bool CheckNoBoldTitle { get; set; }

    public bool CheckCiteTemplateDates { get; set; }

    public bool CheckReorderReferences { get; set; }

    public bool CheckPeopleCategories { get; set; }

    public bool CheckUnbalancedBrackets { get; set; }

    public bool CheckSimpleLinks { get; set; }

    public bool CheckHtmlEntities { get; set; }

    public bool CheckSectionErrors { get; set; }

    public bool CheckUnbulletedLinks { get; set; }

    public bool CheckTypos { get; set; }

    public bool CheckMissingDefaultSort { get; set; }
}
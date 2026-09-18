using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Twain.Core.ReplaceSpecial;

namespace Twain.UI.ReplaceSpecial;

/// <summary>
/// Represents a Replace Special rule in the Avalonia rule tree.
/// </summary>
public partial class ReplaceSpecialRuleViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a rule-tree item for the supplied Replace Special rule.
    /// </summary>
    /// <param name="rule">
    /// The underlying Replace Special rule.
    /// </param>
    public ReplaceSpecialRuleViewModel(IRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        Rule = rule;

        if (rule.Children is null)
            return;

        foreach (IRule childRule in rule.Children)
        {
            Children.Add(
                new ReplaceSpecialRuleViewModel(
                    childRule));
        }
    }

    /// <summary>
    /// Gets the underlying Replace Special rule.
    /// </summary>
    public IRule Rule { get; }

    /// <summary>
    /// Gets the child rules displayed beneath this rule.
    /// </summary>
    public ObservableCollection<ReplaceSpecialRuleViewModel> Children { get; } =
        [];

    /// <summary>
    /// Gets or sets the rule's display name.
    /// </summary>
    public string Name
    {
        get => Rule.Name;

        set
        {
            if (Rule.Name == value)
                return;

            Rule.Name = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets whether the rule is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => Rule.enabled_;

        set
        {
            if (Rule.enabled_ == value)
                return;

            Rule.enabled_ = value;
            OnPropertyChanged();
        }
    }

}
/*
Derived from Autowikibrowser
Copyright (C) 2007 Martin Richards

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
*/

using System.Windows.Forms;
using Twain.Core.Parse;

namespace Twain.Core.ReplaceSpecial;

/// <summary>
/// Defines a replacement rule that can operate on the entire page or within
/// individual template invocations.
/// </summary>
public class Rule : IRule
{
    /// <summary>
    /// Specifies where the rule should be applied.
    /// </summary>
    public enum T
    {
        /// <summary>
        /// Applies the rule to the entire page.
        /// </summary>
        OnWholePage = 0,

        /// <summary>
        /// Applies the rule separately within template invocations.
        /// </summary>
        InsideTemplate
    }

    public T ruletype_ = T.OnWholePage;

    public string replace_ = string.Empty;
    public string with_ = string.Empty;
    public string ifContains_ = string.Empty;
    public string ifNotContains_ = string.Empty;

    public bool regex_;
    public bool ifIsRegex_;

    public int numoftimes_ = 1;

    public RegexOptions ifRegexOptions_ = RegexOptions.None;
    public RegexOptions regexOptions_ = RegexOptions.None;

    private RuleControl? ruleControl_;

    /// <summary>
    /// Initializes a new instance of the <see cref="Rule"/> class.
    /// </summary>
    public Rule()
    {
        Name = "Rule";
    }

    /// <summary>
    /// Creates a shallow copy of this rule without retaining its associated
    /// user interface control.
    /// </summary>
    /// <returns>
    /// A shallow copy of the current rule.
    /// </returns>
    public override object Clone()
    {
        Rule clone = (Rule)MemberwiseClone();
        clone.ruleControl_ = null;

        return clone;
    }

    /// <summary>
    /// Gets the user interface control currently associated with this rule.
    /// </summary>
    /// <returns>
    /// The associated rule control, or <see langword="null"/> if no control is
    /// currently associated with the rule.
    /// </returns>
    public override Control GetControl()
    {
        return ruleControl_;
    }

    /// <summary>
    /// Removes the association between this rule and its current user
    /// interface control.
    /// </summary>
    public override void ForgetControl()
    {
        ruleControl_ = null;
    }

    /// <summary>
    /// Creates and initializes the user interface control for this rule.
    /// </summary>
    /// <param name="owner">
    /// The owner responsible for hosting the rule control.
    /// </param>
    /// <param name="collection">
    /// The control collection that will contain the created control.
    /// </param>
    /// <param name="pos">
    /// The initial location of the control.
    /// </param>
    /// <returns>
    /// The newly created rule control.
    /// </returns>
    public override Control CreateControl(
        IRuleControlOwner owner,
        Control.ControlCollection collection,
        System.Drawing.Point pos)
    {
        RuleControl control = new(owner)
        {
            Location = pos
        };

        control.RestoreFromRule(this);

        DisposeControl();

        ruleControl_ = control;
        collection.Add(control);

        return control;
    }

    /// <summary>
    /// Saves the current rule-control values to this rule.
    /// </summary>
    public override void Save()
    {
        if (ruleControl_ is null)
        {
            return;
        }

        ruleControl_.SaveToRule(this);
    }

    /// <summary>
    /// Restores this rule's values to the associated rule control.
    /// </summary>
    public override void Restore()
    {
        if (ruleControl_ is null)
        {
            return;
        }

        ruleControl_.RestoreFromRule(this);
    }

    /// <summary>
    /// Selects the rule name in the associated rule control.
    /// </summary>
    public override void SelectName()
    {
        if (ruleControl_ is null)
        {
            return;
        }

        ruleControl_.SelectName();
    }

    /// <summary>
    /// Applies this rule to the supplied text the configured number of times.
    /// </summary>
    /// <param name="tn">
    /// The tree node associated with the rule being applied.
    /// </param>
    /// <param name="text">
    /// The text to process.
    /// </param>
    /// <param name="title">
    /// The title of the page being processed.
    /// </param>
    /// <returns>
    /// The processed text, or the original text when the rule is disabled,
    /// the input is empty, or the configured application count is not
    /// positive.
    /// </returns>
    public override string Apply(
        TreeNode tn,
        string text,
        string title)
    {
        if (string.IsNullOrEmpty(text) || !enabled_)
        {
            return text;
        }

        int applyCount = Math.Min(numoftimes_, 100);

        if (applyCount <= 0)
        {
            return text;
        }

        for (int index = 0; index < applyCount; index++)
        {
            text = ApplyOnce(tn, text, title);
        }

        return text;
    }

    /// <summary>
    /// Applies this rule once using the configured rule type.
    /// </summary>
    /// <param name="tn">
    /// The tree node associated with the rule being applied.
    /// </param>
    /// <param name="text">
    /// The text to process.
    /// </param>
    /// <param name="title">
    /// The title of the page being processed.
    /// </param>
    /// <returns>
    /// The processed text.
    /// </returns>
    private static string ApplyOnce(
        TreeNode tn,
        string text,
        string title)
    {
        Rule rule = (Rule)tn.Tag;

        if (rule.ruletype_ == T.OnWholePage)
        {
            return ApplyOn(tn, text, title);
        }

        return rule.ruletype_ == T.InsideTemplate
            ? ApplyInsideTemplate(tn, text, title)
            : text;
    }

    /// <summary>
    /// Applies the rule independently to each template invocation found in the
    /// supplied text, including nested template calls.
    /// </summary>
    /// <param name="tn">
    /// The tree node associated with the rule being applied.
    /// </param>
    /// <param name="text">
    /// The text containing the template invocations to process.
    /// </param>
    /// <param name="title">
    /// The title of the page being processed.
    /// </param>
    /// <returns>
    /// The text after applicable template invocations have been processed.
    /// </returns>
    private static string ApplyInsideTemplate(
        TreeNode tn,
        string text,
        string title)
    {
        string result = text;

        foreach (string template in Parsers.GetAllTemplateDetail(text))
        {
            if (CheckIf(tn, template))
            {
                result = result.Replace(
                    template,
                    ApplyOn(tn, template, title));
            }
        }

        return result;
    }

    /// <summary>
    /// Determines whether the supplied text satisfies the rule's optional
    /// contains and does-not-contain conditions.
    /// </summary>
    /// <param name="tn">
    /// The tree node associated with the rule being evaluated.
    /// </param>
    /// <param name="text">
    /// The text to evaluate.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if all configured conditions are satisfied;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private static bool CheckIf(TreeNode tn, string text)
    {
        Rule rule = (Rule)tn.Tag;

        StringComparison comparison =
            (rule.ifRegexOptions_ & RegexOptions.IgnoreCase) != 0
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        if (!string.IsNullOrEmpty(rule.ifContains_))
        {
            bool contains =
                rule.ifIsRegex_
                    ? Regex.IsMatch(
                        text,
                        rule.ifContains_,
                        rule.ifRegexOptions_)
                    : text.IndexOf(
                        rule.ifContains_,
                        comparison) >= 0;

            if (!contains)
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(rule.ifNotContains_))
        {
            bool containsExcludedText =
                rule.ifIsRegex_
                    ? Regex.IsMatch(
                        text,
                        rule.ifNotContains_,
                        rule.ifRegexOptions_)
                    : text.IndexOf(
                        rule.ifNotContains_,
                        comparison) >= 0;

            if (containsExcludedText)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Applies the configured replacement and then applies any child rules to
    /// the resulting text.
    /// </summary>
    /// <param name="tn">
    /// The tree node associated with the rule being applied.
    /// </param>
    /// <param name="text">
    /// The text to process.
    /// </param>
    /// <param name="title">
    /// The title of the page being processed.
    /// </param>
    /// <returns>
    /// The processed text after the configured replacement and child rules
    /// have been applied.
    /// </returns>
    private static string ReplaceOn(
        TreeNode tn,
        string text,
        string title)
    {
        Rule rule = (Rule)tn.Tag;

        if (!string.IsNullOrEmpty(rule.replace_))
        {
            string pattern =
                Tools.ApplyKeyWords(
                    title,
                    rule.replace_,
                    true);

            string replacement =
                Tools.ApplyKeyWords(
                    title,
                    rule.with_);

            if (!rule.regex_)
            {
                pattern = Regex.Escape(pattern);
            }

            // Convert escaped newline sequences into actual newline
            // characters before applying the replacement.
            replacement = replacement
                .Replace(@"\r", "\r")
                .Replace(@"\n", "\n");

            text = Regex.Replace(
                text,
                pattern,
                replacement,
                rule.regexOptions_);
        }

        foreach (TreeNode childNode in tn.Nodes)
        {
            IRule childRule = (IRule)childNode.Tag;
            text = childRule.Apply(childNode, text, title);
        }

        return text;
    }

    /// <summary>
    /// Applies this rule when the supplied text satisfies its configured
    /// conditions.
    /// </summary>
    /// <param name="tn">
    /// The tree node associated with the rule being applied.
    /// </param>
    /// <param name="text">
    /// The text to process.
    /// </param>
    /// <param name="title">
    /// The title of the page being processed.
    /// </param>
    /// <returns>
    /// The processed text when the conditions are satisfied; otherwise, the
    /// original text.
    /// </returns>
    private static string ApplyOn(
        TreeNode tn,
        string text,
        string title)
    {
        return CheckIf(tn, text)
            ? ReplaceOn(tn, text, title)
            : text;
    }
}
/*
    Copyright (C) 2007 Martin Richards

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System.Windows.Forms;

namespace Twain.Core.ReplaceSpecial;

/// <summary>
/// Defines a replacement rule that operates within one or more specified
/// templates.
/// </summary>
public class InTemplateRule : IRule
{
    /// <summary>
    /// The XML element name used when serializing this rule.
    /// </summary>
    public const string XmlName = "InTemplateRule";

    /// <summary>
    /// Gets or sets the template names to which this rule applies.
    /// </summary>
    public List<string> TemplateNames_ = new();

    /// <summary>
    /// Gets or sets the replacement text applied by this rule.
    /// </summary>
    public string ReplaceWith_ = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether template replacement is
    /// enabled for this rule.
    /// </summary>
    public bool DoReplace_;

    /// <summary>
    /// The WinForms editor control currently associated with this rule.
    /// </summary>
    /// <remarks>
    /// This field is not serialized and is recreated when the rule editor is
    /// displayed.
    /// </remarks>
    private InTemplateRuleControl? ruleControl_;

    /// <summary>
    /// Creates a copy of this rule without its associated user interface
    /// control.
    /// </summary>
    /// <returns>
    /// A shallow copy of the current rule.
    /// </returns>
    public override object Clone()
    {
        InTemplateRule clone = (InTemplateRule)MemberwiseClone();
        clone.ruleControl_ = null;

        return clone;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InTemplateRule"/> class.
    /// </summary>
    public InTemplateRule()
    {
        Name = "In Template Rule";
    }

    /// <summary>
    /// Gets the user interface control associated with this rule.
    /// </summary>
    /// <returns>
    /// The rule control, or <see langword="null"/> if no control is currently
    /// associated with the rule.
    /// </returns>
    public override Control GetControl()
    {
        return ruleControl_;
    }

    /// <summary>
    /// Removes the association between this rule and its current user interface
    /// control.
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
        InTemplateRuleControl rc = new(owner)
        {
            Location = pos
        };

        rc.RestoreFromRule(this);

        DisposeControl();

        ruleControl_ = rc;
        collection.Add(rc);

        return rc;
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
    /// Applies this rule to the supplied text for each configured template name.
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
    /// The processed text, or the original text when the rule is disabled or the
    /// input text is empty.
    /// </returns>
    public override string Apply(
        TreeNode tn,
        string text,
        string title)
    {
        if (!enabled_ || string.IsNullOrEmpty(text))
        {
            return text;
        }

        foreach (string template in TemplateNames_)
        {
            text = ApplyInsideTemplate(
                template,
                tn,
                text,
                title);
        }

        return text;
    }

    /// <summary>
    /// Parses and processes matching template invocations within article text.
    /// </summary>
    private sealed class ParseTemplate
    {
        private readonly string template_;
        private string text_;
        private readonly string title_;
        private string result_ = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseTemplate"/> class.
        /// </summary>
        /// <param name="template">
        /// The template name to locate and process.
        /// </param>
        /// <param name="text">
        /// The text containing the template invocations.
        /// </param>
        /// <param name="title">
        /// The title of the page being processed.
        /// </param>
        public ParseTemplate(
            string template,
            string text,
            string title)
        {
            template_ = template;
            text_ = text;
            title_ = title;
        }

        /// <summary>
        /// Gets the processed text.
        /// </summary>
        public string Result =>
            result_;

        /// <summary>
        /// Applies the supplied rule tree to each matching template invocation.
        /// </summary>
        /// <param name="tn">
        /// The tree node containing the rules to apply.
        /// </param>
        public void Parse(TreeNode tn)
        {
            List<string> templateCalls =
                Twain.Core.Parse.Parsers.GetAllTemplateDetail(text_);

            templateCalls.RemoveAll(
                templateCall =>
                    Tools.TurnFirstToUpperNoProjectCheck(
                        Tools.GetTemplateName(templateCall)) !=
                    Tools.TurnFirstToUpperNoProjectCheck(template_));

            foreach (string templateCall in templateCalls)
            {
                string replacement =
                    ReplaceOn(
                        template_,
                        tn,
                        templateCall,
                        title_);

                text_ = text_.Replace(
                    templateCall,
                    replacement);
            }

            result_ = text_;
        }
    }

    /// <summary>
    /// Parses the supplied text within the context of the specified template and
    /// applies the rule tree to the parsed template content.
    /// </summary>
    /// <param name="template">
    /// The template name to locate and process.
    /// </param>
    /// <param name="tn">
    /// The tree node containing the rules to apply.
    /// </param>
    /// <param name="text">
    /// The text containing the template invocation.
    /// </param>
    /// <param name="title">
    /// The title of the page being processed.
    /// </param>
    /// <returns>
    /// The processed text returned by the template parser.
    /// </returns>
    private static string ApplyInsideTemplate(
        string template,
        TreeNode tn,
        string text,
        string title)
    {
        ParseTemplate parser = new(template, text, title);

        parser.Parse(tn);

        return parser.Result;
    }

    /// <summary>
    /// Checks the input text for the input template
    /// </summary>
    /// <param name="template">The template name</param>
    /// <param name="text">The template text</param>
    /// <returns>whether the input template name is used in the input text</returns>
    public static bool TemplateUsedInText(string template, string text)
    {
        if (string.IsNullOrEmpty(template))
            return true;

        // allow match on spaces or underscores
        string pattern = @"^\s*" + Tools.FirstLetterCaseInsensitive(template).Replace(" ", "[ _]+") + @"\s*(?:}}|\|)";

        // don't match on comments
        text = WikiRegexes.Comments.Replace(text, "");

        return Regex.IsMatch(text, pattern);
    }

    /// <summary>
    /// Applies child rules to the supplied text and optionally replaces the
    /// matched template name.
    /// </summary>
    /// <param name="template">
    /// The template name to replace.
    /// </param>
    /// <param name="tn">
    /// The tree node associated with the current rule.
    /// </param>
    /// <param name="text">
    /// The text being processed.
    /// </param>
    /// <param name="title">
    /// The title of the page being processed.
    /// </param>
    /// <returns>
    /// The processed text after child rules and any configured template-name
    /// replacement have been applied.
    /// </returns>
    private static string ReplaceOn(
        string template,
        TreeNode tn,
        string text,
        string title)
    {
        InTemplateRule rule = (InTemplateRule)tn.Tag;

        foreach (TreeNode childNode in tn.Nodes)
        {
            IRule childRule = (IRule)childNode.Tag;
            text = childRule.Apply(childNode, text, title);
        }

        if (!rule.DoReplace_ ||
            string.IsNullOrEmpty(rule.ReplaceWith_) ||
            string.IsNullOrEmpty(template))
        {
            return text;
        }

        // TODO: Review the template replacement regex with focused tests, including
        // comments, whitespace, underscores, pipes, and closing braces.
        string pattern =
            @"^([\s]*)" +
            Tools.FirstLetterCaseInsensitive(template) +
            @"([\s]*(?:<!--.*-->)?[\s]*(\}\}|\|))";

        pattern = pattern.Replace(" ", "[ _]+");

        string replacement =
            Tools.ApplyKeyWords(
                title,
                rule.ReplaceWith_,
                false);

        return Regex.Replace(
            text,
            pattern,
            "$1" + replacement + "$2");
    }
}
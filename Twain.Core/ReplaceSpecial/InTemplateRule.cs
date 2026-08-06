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

public class InTemplateRule : IRule
{
    public const string XmlName = "InTemplateRule";

    public List<string> TemplateNames_ = new List<string>();
    public string ReplaceWith_ = "";
    public bool DoReplace_;

    InTemplateRuleControl ruleControl_;

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

    class ParseTemplate
    {
        readonly string template_;
        string text_;
        readonly string title_;
        string result_ = "";

        public ParseTemplate(string template, string text, string title)
        {
            template_ = template;
            text_ = text;
            title_ = title;
        }

        public string Result { get { return result_; } }

        public void Parse(TreeNode tn)
        {
            // get all template calls in text, including nested
            List<string> allT = Twain.Core.Parse.Parsers.GetAllTemplateDetail(text_);

            // only need to process template calls that match the input template name
            allT.RemoveAll(t => Tools.TurnFirstToUpperNoProjectCheck(Tools.GetTemplateName(t)) != Tools.TurnFirstToUpperNoProjectCheck(template_));

            allT.ForEach(t =>
            {
                string res = ReplaceOn(template_, tn, t, title_);
                text_ = text_.Replace(t, res);
            });

            result_ = text_;
        }
    }

    private static string ApplyInsideTemplate(string template, TreeNode tn, string text, string title)
    {
        ParseTemplate p = new ParseTemplate(template, text, title);

        p.Parse(tn);

        return p.Result;
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

    private static string ReplaceOn(string template, TreeNode tn, string text, string title)
    {
        InTemplateRule r = (InTemplateRule)tn.Tag;

        foreach (TreeNode t in tn.Nodes)
        {
            IRule sr = (IRule)t.Tag;
            text = sr.Apply(t, text, title);
        }

        if (r.DoReplace_ && !string.IsNullOrEmpty(r.ReplaceWith_))
        {
            if (string.IsNullOrEmpty(template))
                return text;

            string pattern =
              @"^([\s]*)" + Tools.FirstLetterCaseInsensitive(template) + @"([\s]*(?:<!--.*-->)?[\s]*(\}\}|\|))";

            pattern = pattern.Replace(" ", "[ _]+");

            string replace = Tools.ApplyKeyWords(title, r.ReplaceWith_, false);

            text = Regex.Replace(text, pattern, "$1" + replace + "$2");
        }

        return text;
    }
}
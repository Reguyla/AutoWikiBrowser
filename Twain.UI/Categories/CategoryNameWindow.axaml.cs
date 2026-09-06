using Avalonia.Interactivity;

namespace Twain.UI.Categories;

/// <summary>
/// Prompts the user to enter the name of a category.
/// </summary>
/// <remarks>
/// The category namespace is supplied by the caller so the window remains
/// independent of the current wiki session and namespace configuration.
/// </remarks>
public partial class CategoryNameWindow : Avalonia.Controls.Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryNameWindow"/>
    /// class using preview-safe values.
    /// </summary>
    public CategoryNameWindow()
        : this("Category:", string.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryNameWindow"/> class.
    /// </summary>
    /// <param name="categoryNamespace">
    /// The localized category namespace prefix to display.
    /// </param>
    /// <param name="categoryName">
    /// The previously entered category name, if any.
    /// </param>
    public CategoryNameWindow(
        string categoryNamespace,
        string categoryName = "")
    {
        ArgumentNullException.ThrowIfNull(categoryNamespace);
        ArgumentNullException.ThrowIfNull(categoryName);

        InitializeComponent();

        CategoryNamespaceTextBlock.Text =
            categoryNamespace;

        CategoryNameTextBox.Text =
            categoryName;

        Opened += CategoryNameWindow_Opened;
    }

    /// <summary>
    /// Gets the fully qualified category page name entered by the user.
    /// </summary>
    /// <value>
    /// The supplied category namespace followed by the entered category
    /// name, or an empty string when no category name was supplied.
    /// </value>
    public string CategoryName
    {
        get
        {
            string categoryName =
                CategoryNameTextBox.Text ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return string.Empty;
            }

            return
                (CategoryNamespaceTextBlock.Text ?? string.Empty) +
                categoryName;
        }
    }

    /// <summary>
    /// Selects an existing category name when the window is opened so it
    /// can be replaced immediately.
    /// </summary>
    private void CategoryNameWindow_Opened(
        object? sender,
        EventArgs e)
    {
        if (!string.IsNullOrEmpty(CategoryNameTextBox.Text))
        {
            CategoryNameTextBox.SelectAll();
        }

        CategoryNameTextBox.Focus();
    }

    /// <summary>
    /// Accepts the entered category name and closes the window.
    /// </summary>
    private void OkButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }
}
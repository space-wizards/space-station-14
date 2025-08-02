using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Client.UserInterface.RichText;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Robust.Shared.IoC;
using Content.Client.Paper.UI;

namespace Content.Client.UserInterface.RichText;

/// <summary>
/// Converts [signature] tags into clickable buttons that sign with the player's name.
/// </summary>
public sealed class SignatureTagHandler : IMarkupTagHandler
{
    public string Name => "signature";
    private static int _signatureCounter = 0;

    private static int GetSignatureIndex(MarkupNode node)
    {
        return _signatureCounter++;
    }

    /// <summary>
    /// Resets the signature counter to ensure consistent indexing across renders.
    /// </summary>
    public static void ResetSignatureCounter()
    {
        _signatureCounter = 0;
    }

    /// <summary>
    /// Counts signature buttons before the clicked button to determine which [signature] tag it represents.
    /// </summary>
    private static int CountSignatureButtonsBefore(Control clickedButton)
    {
        var count = 0;
        var root = clickedButton;

        // Find the root container
        while (root.Parent != null)
            root = root.Parent;

        // Count signature buttons in document order
        var found = false;
        CountSignatureButtonsRecursive(root, clickedButton, ref count, ref found);
        return found ? count : 0;
    }

    private static void CountSignatureButtonsRecursive(Control control, Control target, ref int count, ref bool found)
    {
        if (found) return;

        if (control is Button btn && btn.Text == Loc.GetString("paper-signature-sign-button"))
        {
            if (control == target)
            {
                found = true;
                return;
            }
            count++;
        }

        foreach (Control child in control.Children)
        {
            CountSignatureButtonsRecursive(child, target, ref count, ref found);
        }
    }

    public SignatureTagHandler()
    {
        IoCManager.InjectDependencies(this);
    }

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context) { }
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context) { }
    public string TextBefore(MarkupNode node) => "";
    public string TextAfter(MarkupNode node) => "";

    /// <summary>
    /// Creates a clickable signature button to replace the [signature] tag.
    /// </summary>
    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        var btn = new Button
        {
            Text = Loc.GetString("paper-signature-sign-button"),
            MinSize = new Vector2(60, 28),
            MaxSize = new Vector2(60, 28),
            Margin = new Thickness(4, 2, 4, 2)
        };

        var signatureIndex = GetSignatureIndex(node);
        btn.Name = $"signature_{signatureIndex}";

        btn.OnPressed += _ =>
        {
            // Find the PaperWindow parent
            var parent = btn.Parent;
            while (parent != null && parent is not PaperWindow)
                parent = parent.Parent;

            if (parent is PaperWindow paperWindow)
            {
                // Count buttons to determine which [signature] tag this represents
                var buttonIndex = CountSignatureButtonsBefore(btn);
                // Send signature request to server instead of handling client-side
                paperWindow.SendSignatureRequest(buttonIndex);
            }
        };

        control = btn;
        return true;
    }


}

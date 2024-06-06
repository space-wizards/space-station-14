using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Input;
using Robust.Shared.Utility;

namespace Content.Client.Guidebook.RichText;

[UsedImplicitly]
public sealed class TextLinkTag : IMarkupTag
{
    public string Name => "textlink";

    public Control? Control;
    public bool ShouldAddSelfAsChild = true;

    /// <inheritdoc/>
    public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Value.TryGetString(out var text)
            || !node.Attributes.TryGetValue("link", out var linkParameter)
            || !linkParameter.TryGetString(out var link))
        {
            control = null;
            return false;
        }

        if (node.Attributes.TryGetValue("addAsChild", out var shouldAddAsChild) &&
            !shouldAddAsChild.TryGetString(out var addAsChild) &&
            bool.TryParse(addAsChild, out var value))
        {
            ShouldAddSelfAsChild = value;
        }

        var label = new Label();
        label.Text = text;

        label.MouseFilter = Control.MouseFilterMode.Stop;
        label.FontColorOverride = Color.CornflowerBlue;
        label.DefaultCursorShape = Control.CursorShape.Hand;

        label.OnMouseEntered += args => OnMouseEntered(args, link);
        label.OnMouseExited += _ => label.FontColorOverride = Color.CornflowerBlue;
        label.OnKeyBindDown += args => OnKeybindDown(args, link);

        control = label;
        Control = label;
        return true;
    }

    private void OnMouseEntered(GUIMouseHoverEventArgs obj, string link)
    {
        if (Control is not Label label)
            return;

        label.FontColorOverride = Color.LightSkyBlue;

        if (label.Parent is not ILinkHandler handler)
            return;
    }

    private void OnKeybindDown(GUIBoundKeyEventArgs args, string link)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (Control == null)
            return;

        var current = Control;
        while (current != null)
        {
            current = current.Parent;

            if (current is not ILinkHandler handler)
                continue;
            handler.HandleClick(link);
            return;
        }
        Logger.Warning($"Warning! No valid ILinkClickHandler found.");
    }
}

public interface ILinkHandler
{
    public void HandleLinkAsChild(string link);

    public void HandleClick(string link);
}

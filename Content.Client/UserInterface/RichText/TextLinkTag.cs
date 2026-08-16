using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Content.Shared.Chat;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Input;
using Robust.Shared.Utility;
using Content.Client.UserInterface.ControlExtensions;

namespace Content.Client.UserInterface.RichText;

[UsedImplicitly]
public sealed class TextLinkTag : IMarkupTagHandler
{
    [Dependency] private IEntityManager _entity = default!;

    public static Color LinkColor => Color.CornflowerBlue;

    public string Name => "textlink";

    public string TextBefore(MarkupNode node)
    {
        if (!node.Attributes.TryGetValue("ent", out var entParam) || !entParam.TryGetString(out _))
            return string.Empty;

        if (!node.Value.TryGetString(out var text))
            return string.Empty;

        var chat = _entity.System<SharedChatSystem>();
        return chat.CanClickMessageSender(null) ? string.Empty : text;
    }

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Value.TryGetString(out var text))
        {
            control = null;
            return false;
        }

        string link;

        if (node.Attributes.TryGetValue("ent", out var entParam) && entParam.TryGetString(out var entStr))
        {
            if (!NetEntity.TryParse(entStr, out var netEntity))
            {
                control = null;
                return false;
            }

            var chat = _entity.System<SharedChatSystem>();
            if (!chat.CanClickMessageSender(null))
            {
                control = null;
                return false;
            }

            link = netEntity.ToString();
        }
        else if (node.Attributes.TryGetValue("link", out var linkParam) && linkParam.TryGetString(out var linkStr))
        {
            link = linkStr;
        }
        else
        {
            control = null;
            return false;
        }

        var label = new Label { Text = text };
        label.MouseFilter = Control.MouseFilterMode.Stop;
        label.FontColorOverride = LinkColor;
        label.DefaultCursorShape = Control.CursorShape.Hand;
        label.OnMouseEntered += _ => label.FontColorOverride = Color.LightSkyBlue;
        label.OnMouseExited += _ => label.FontColorOverride = LinkColor;
        label.OnKeyBindDown += args => OnKeybindDown(args, link, label);

        control = label;
        return true;
    }

    private void OnKeybindDown(GUIBoundKeyEventArgs args, string link, Control? control)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (control == null)
            return;

        if (control.TryGetParentHandler<ILinkClickHandler>(out var handler))
            handler.HandleClick(link);
        else
            Logger.Warning("Warning! No valid ILinkClickHandler found.");
    }
}

public interface ILinkClickHandler
    {
        public void HandleClick(string link);
    }


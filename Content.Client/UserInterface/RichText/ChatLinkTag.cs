using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chat;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Input;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.RichText;

public sealed partial class ChatLinkTag : IMarkupTagHandler
{
    [Dependency] private IEntityManager _entity = default!;

    public const string TagName = "chatlink";
    public const string EntAttributeName = "ent";

    public string Name => TagName;

    public string TextBefore(MarkupNode node)
    {
        if (!node.Value.TryGetString(out var text))
            return string.Empty;

        var sys = _entity.System<SharedChatSystem>();
        return sys.CanClickMessageSender(null) ? string.Empty : text;
    }

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Value.TryGetString(out var text) ||
            !node.Attributes.TryGetValue(EntAttributeName, out var entParameter) ||
            !entParameter.TryGetString(out var entStr) ||
            !NetEntity.TryParse(entStr, out var ent))
        {
            control = null;
            return false;
        }

        var label = new ChatLinkLabel();
        label.Text = text;
        label.OnMouseEntered += _ => label.FontColorOverride = Color.Blue;
        label.OnMouseExited += _ => label.FontColorOverride = Color.LightBlue;
        label.OnKeyBindDown += args => OnKeyBindDown(args, ent);

        control = label;
        return true;
    }

    private void OnKeyBindDown(GUIBoundKeyEventArgs args, NetEntity ent)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        var ev = new ClickMessageSenderRequestEvent(ent);
        _entity.RaisePredictiveEvent(ev);
    }
}


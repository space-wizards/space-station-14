using System.Diagnostics.CodeAnalysis;
using Content.Client.RichText;
using Content.Shared.Chat;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Content.Client.UserInterface.ControlExtensions;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Input;
using Robust.Shared.Utility;


namespace Content.Client.UserInterface.RichText;

public sealed partial class ChatLinkTag : IMarkupTagHandler
{
    [Dependency] private IEntityManager _entity = default!;

    public const string TagName = "chatlink";

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
        if (!node.Value.TryGetString(out var name)
            || !node.Attributes.TryGetValue("ent", out var entParam)
            || !entParam.TryGetString(out var entStr)
            || !NetEntity.TryParse(entStr, out var netEntity))
        {
            control = null;
            return false;
        }
        var sys = _entity.System<SharedChatSystem>();
        if (!sys.CanClickMessageSender(null))
        {
            control = null;
            return false;
        }

        var label = new Label(){ Text = name };
        label.MouseFilter = Control.MouseFilterMode.Stop;
        label.FontColorOverride = Color.LightBlue;
        label.DefaultCursorShape = Control.CursorShape.Hand;

        label.OnMouseEntered += _ => label.FontColorOverride = Color.Blue;
        label.OnMouseExited += _ => label.FontColorOverride = Color.LightBlue;
        label.OnKeyBindDown += args => OnKeyBindDown(args, netEntity, label);

        control = label;
        return true;
    }

    private void OnKeyBindDown(GUIBoundKeyEventArgs args, NetEntity ent, Control? control)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (control == null)
            return;

        if (control.TryGetParentHandler<ILinkClickHandler>(out var handler))
            handler.HandleClick(ent.ToString());
    }
}


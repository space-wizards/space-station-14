using System.Diagnostics.CodeAnalysis;
using Content.Client.UserInterface.ControlExtensions;
using JetBrains.Annotations;
using Content.Shared.Chat;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Utility;
using Content.Client.UserInterface.Systems.Chat;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Client.UserInterface.RichText;

[UsedImplicitly]
public sealed partial class TextLinkTag : IMarkupTagHandler
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    public static Color LinkColor => Color.CornflowerBlue;

    public string Name => "textlink";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Value.TryGetString(out var text))
        {
            control = null;
            return false;
        }

        string link;
        var baseColor = LinkColor;
        var clickable = false;

        if (node.Attributes.TryGetValue("ent", out var entParam) && entParam.TryGetString(out var entStr))
        {
            if (!NetEntity.TryParse(entStr, out var netEntity))
            {
                control = null;
                return false;
            }

            if (GetEntityLinkColor(netEntity) is { } entColor)
                baseColor = entColor;

            var chat = _entity.System<SharedChatSystem>();
            clickable = chat.CanClickMessageSender(null);

            link = netEntity.ToString();
        }
        else if (node.Attributes.TryGetValue("link", out var linkParam) && linkParam.TryGetString(out var linkStr))
        {
            link = linkStr;
            clickable = true; // plain guidebook-style links are always clickable
        }
        else
        {
            control = null;
            return false;
        }

        var label = new Label { Text = text };
        label.FontColorOverride = baseColor;

        if (clickable)
        {
            label.MouseFilter = Control.MouseFilterMode.Stop;
            label.DefaultCursorShape = Control.CursorShape.Hand;
            label.OnMouseEntered += _ => label.FontColorOverride = Color.LightSkyBlue;
            label.OnMouseExited += _ => label.FontColorOverride = baseColor;
            label.OnKeyBindDown += args => OnKeybindDown(args, link, label);
        }

        control = label;
        return true;
    }

private Color? GetEntityLinkColor(NetEntity netEntity)
{
    var chatUi = _ui.GetUIController<ChatUIController>();

    if (!chatUi.ChatNameColorsEnabled)
        return null;

    if (!_entity.TryGetEntity(netEntity, out var uid) || !_entity.EntityExists(uid))
        return null;

    if (!_entity.TryGetComponent<GrammarComponent>(uid, out var grammar) || grammar.ProperNoun != true)
        return null;

    var name = _entity.GetComponent<MetaDataComponent>(uid.Value).EntityName;
    return Color.FromHex(chatUi.GetNameColor(name));
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


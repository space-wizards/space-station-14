using Content.Shared.Chat;
using Content.Client.UserInterface.Systems.Chat;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.RichText;

public sealed partial class TextLinkTag
{
    private bool TryResolveEntityLink(MarkupNode node, out TextLinkData data)
    {
        data = default;

        if (!node.Attributes.TryGetValue(EntAttributeName, out var entParam) ||
            !entParam.TryGetString(out var entStr))
        {
            return false;
        }

        if (!NetEntity.TryParse(entStr, out var netEntity))
            return false;

        var chat = _entity.System<SharedChatSystem>();
        var clickable = chat.CanClickMessageSender(null);
        var color = GetEntityNameColor(node, netEntity);

        data = new TextLinkData(netEntity.ToString(), color, clickable);
        return true;
    }

    private Color? GetEntityNameColor(MarkupNode node, NetEntity netEntity)
    {
        if (!node.Attributes.TryGetValue(ColorableAttributeName, out var colorableParam) ||
            !colorableParam.TryGetString(out var colorableStr) ||
            !bool.TryParse(colorableStr, out var colorable) ||
            !colorable)
        {
            return null;
        }

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
}

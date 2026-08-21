using Content.Shared.Chat;
using Content.Client.UserInterface.Systems.Chat;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.RichText;

public sealed partial class TextLinkTag
{
    /// <summary>
    ///entity="<NetEntity>" resolver. Clickable only if the local viewer is
    /// currently allowed to click chat names.
    /// </summary>
    private bool TryResolveEntityLink(MarkupNode node, out LinkData data)
    {
        data = default;

        if (!node.Attributes.TryGetValue(EntityAttributeName, out var entParam) ||
            !entParam.TryGetString(out var entStr))
        {
            return false;
        }

        if (!NetEntity.TryParse(entStr, out var netEntity))
            return false;

        var chat = _entity.System<SharedChatSystem>();
        var clickable = chat.CanClickMessageSender(null);

        var color = GetLinkColor(node, netEntity);

        data = new LinkData(LinkString: null, LinkEntity: netEntity, Color: color, Clickable: clickable);
        return true;
    }

    /// <summary>
    /// Sets LinkColor if TextLink param and ChatNameColors Cvar allow.
    /// </summary>
    private Color? GetLinkColor(MarkupNode node, NetEntity netEntity)
    {
        if (!node.Attributes.TryGetValue(UseEntityNameColorAttributeName, out var useNameColorParam) ||
            !useNameColorParam.TryGetString(out var useNameColorStr) ||
            !bool.TryParse(useNameColorStr, out var useNameColor) ||
            !useNameColor)
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

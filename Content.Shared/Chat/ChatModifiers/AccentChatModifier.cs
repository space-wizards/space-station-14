using Content.Shared.Chat.Prototypes;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Inserts the [Accent="accent"] tag inside of the [MainMessage] tag.
/// Accent tags are processed clientside to enable accessibility options/admins turning them off.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class AccentChatModifier : ChatModifier
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IEntitySystemManager _entSys = default!;

    public override FormattedMessage ProcessChatModifier(FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        IoCManager.InjectDependencies(this);

        if (_entSys.TryGetEntitySystem<SharedAccentSystem>(out var accentSystem) &&
            channelParameters.TryGetValue(DefaultChannelParameters.SenderEntity, out var senderEntity))
        {
            var accents = accentSystem.GetAccentList((EntityUid)senderEntity);
            foreach (var accentName in accents)
            {
                message.InsertInsideTag(new MarkupNode("Accent", new MarkupParameter(accentName), null, false), "MainMessage");
            }
        }

        return message;
    }
}

using Content.Shared.Mind;
using Content.Shared.Roles.RoleCodeword;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.ChatModifiers;

/// <summary>
/// Inserts the Codewords content tag inside of an existing MainMessage tag, making the message colored. 
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class CodewordsColorChatModifier : ChatModifier
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IEntitySystemManager _entSys = default!;

    public override FormattedMessage ProcessChatModifier(FormattedMessage message, ChatMessageContext chatMessageContext)
    {
        IoCManager.InjectDependencies(this);

        if (_entSys.TryGetEntitySystem<SharedMindSystem>(out var mindSystem) &&
            _player.LocalUser != null && mindSystem.TryGetMind(_player.LocalUser.Value, out var mindId) &&
            _ent.TryGetComponent(mindId, out RoleCodewordComponent? codewordComp)
        )
            message.InsertInsideTag(new MarkupNode("Codewords", null, null), "MainMessage");

        return message;

    }
}

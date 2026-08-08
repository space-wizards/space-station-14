using Content.Shared.Chat;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SayKeyOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private SharedChatSystem _chat = default!;

    [DataField(required: true)]
    public string Key = string.Empty;

    /// <summary>
    /// Whether to hide message from chat window and logs.
    /// </summary>
    [DataField]
    public bool Hidden;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<object>(Key, out var value, _entManager))
            return HTNOperatorStatus.Failed;

        var @string = value.ToString();
        if (@string is not { })
            return HTNOperatorStatus.Failed;

        var speaker = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        _chat.TrySendInGameICMessage(speaker, @string, InGameICChatType.Speak, hideChat: Hidden, hideLog: Hidden);

        return base.Update(blackboard, frameTime);
    }
}

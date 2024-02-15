using Content.Server.Chat.Systems;
using Content.Server.Chat.V2;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SpeakOperator : HTNOperator
{
    private ChatSystem _chat = default!;

    [DataField("speech", required: true)]
    public string Speech = string.Empty;

    /// <summary>
    /// Whether to hide message from chat window and logs. This does nothing currently.
    /// </summary>
    [DataField]
    public bool Hidden;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _chat = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var speaker = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        _chat.SendBackgroundChatMessage(speaker, Loc.GetString(Speech));

        return base.Update(blackboard, frameTime);
    }
}

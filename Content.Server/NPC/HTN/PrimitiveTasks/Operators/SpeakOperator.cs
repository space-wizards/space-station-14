using Content.Server.Chat.Systems;
using Robust.Shared.Timing;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SpeakOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    private ChatSystem _chat = default!;

    [DataField(required: true)]
    public string Speech = string.Empty;

    /// <summary>
    /// Whether to hide message from chat window and logs.
    /// </summary>
    [DataField]
    public bool Hidden;

    /// <summary>
    /// Skip speaking for `cooldown` seconds, intended to stop spam
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.Zero;

    /// <summary>
    /// Define what key is used for storing the cooldown
    /// </summary>
    [DataField]
    public string CooldownID = String.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);

        _chat = sysManager.GetEntitySystem<ChatSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (Cooldown != TimeSpan.Zero && CooldownID != string.Empty)
        {
            if(blackboard.TryGetValue<TimeSpan>(CooldownID, out var nextSpeechTime, _entMan))
                if(_gameTiming.CurTime < nextSpeechTime)
                    return base.Update(blackboard, frameTime);

            blackboard.SetValue(CooldownID, _gameTiming.CurTime + Cooldown);
        }

        var speaker = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        _chat.TrySendInGameICMessage(speaker, Loc.GetString(Speech), InGameICChatType.Speak, hideChat: Hidden, hideLog: Hidden);

        return base.Update(blackboard, frameTime);
    }
}

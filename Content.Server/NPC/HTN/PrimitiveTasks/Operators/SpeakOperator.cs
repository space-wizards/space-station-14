using Content.Server.Chat.Systems;
using Robust.Shared.Timing;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using static Content.Server.NPC.HTN.PrimitiveTasks.Operators.SpeakOperator.SpeakOperatorSpeech;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SpeakOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    private ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [DataField(required: true)]
    public SpeakOperatorSpeech Speech;

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
    public string CooldownID = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _chat = sysManager.GetEntitySystem<ChatSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (Cooldown != TimeSpan.Zero && CooldownID != string.Empty)
        {
            if (blackboard.TryGetValue<TimeSpan>(CooldownID, out var nextSpeechTime, _entMan) && _gameTiming.CurTime < nextSpeechTime)
                return base.Update(blackboard, frameTime);

            blackboard.SetValue(CooldownID, _gameTiming.CurTime + Cooldown);
        }

        LocId speechLocId;
        switch (Speech)
        {
            case LocalizedSetSpeakOperatorSpeech localizedDataSet:
                if (!_proto.TryIndex(localizedDataSet.LineSet, out var speechSet))
                    return HTNOperatorStatus.Failed;
                speechLocId = _random.Pick(speechSet);
                break;
            case SingleSpeakOperatorSpeech single:
                speechLocId = single.Line;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Speech));
        }

        var speaker = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        _chat.TrySendInGameICMessage(
            speaker,
            Loc.GetString(speechLocId),
            InGameICChatType.Speak,
            hideChat: Hidden,
            hideLog: Hidden
        );

        return base.Update(blackboard, frameTime);
    }

    [ImplicitDataDefinitionForInheritors, MeansImplicitUse]
    public abstract partial class SpeakOperatorSpeech
    {
        public sealed partial class SingleSpeakOperatorSpeech : SpeakOperatorSpeech
        {
            [DataField(required: true)]
            public string Line;
        }

        public sealed partial class LocalizedSetSpeakOperatorSpeech : SpeakOperatorSpeech
        {
            [DataField(required: true)]
            public ProtoId<LocalizedDatasetPrototype> LineSet;
        }
    }
}

using Content.Server.Chat.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.NPC.Components;
using Content.Shared.Damage;
using Content.Shared.Emag.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Silicons.Bots;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed class MedibotInjectOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private SharedAudioSystem _audio = default!;
    private ChatSystem _chat = default!;
    private SharedInteractionSystem _interaction = default!;
    private SharedPopupSystem _popup = default!;
    private SolutionContainerSystem _solution = default!;

    /// <summary>
    /// Target entity to inject.
    /// </summary>
    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _audio = sysManager.GetEntitySystem<SharedAudioSystem>();
        _chat = sysManager.GetEntitySystem<ChatSystem>();
        _interaction = sysManager.GetEntitySystem<SharedInteractionSystem>();
        _popup = sysManager.GetEntitySystem<SharedPopupSystem>();
        _solution = sysManager.GetEntitySystem<SolutionContainerSystem>();
    }

    public override void Shutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.Shutdown(blackboard, status);
        blackboard.Remove<EntityUid>(TargetKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        // TODO: Wat
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entMan) || _entMan.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (!_entMan.TryGetComponent<MedibotComponent>(owner, out var botComp))
            return HTNOperatorStatus.Failed;

        // To avoid spam, the rest of this needs fixing.
        _entMan.EnsureComponent<NPCRecentlyInjectedComponent>(target);

        if (!_entMan.TryGetComponent<DamageableComponent>(target, out var damage))
            return HTNOperatorStatus.Failed;

        if (!_solution.TryGetInjectableSolution(target, out var injectable))
            return HTNOperatorStatus.Failed;

        if (!_interaction.InRangeUnobstructed(owner, target))
            return HTNOperatorStatus.Failed;

        // if emagged, always treat below-crit as injured (give funny juice to healthy people)
        var total = damage.TotalDamage;
        if (_entMan.HasComponent<EmaggedComponent>(owner) && total < MedibotComponent.EmergencyMedDamageThreshold)
        {
            total = MedibotComponent.EmergencyMedDamageThreshold;
        }

        if (total == 0)
            return HTNOperatorStatus.Failed;

        if (total >= MedibotComponent.EmergencyMedDamageThreshold)
        {
            _solution.TryAddReagent(target, injectable, botComp.EmergencyMed, botComp.EmergencyMedAmount, out var accepted);
            _popup.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, target);
            _audio.PlayPvs(botComp.InjectSound, target);
            _chat.TrySendInGameICMessage(owner, Loc.GetString("medibot-finish-inject"), InGameICChatType.Speak, ChatTransmitRange.GhostRangeLimit);
            return HTNOperatorStatus.Finished;
        }

        if (total >= MedibotComponent.StandardMedDamageThreshold)
        {
            _solution.TryAddReagent(target, injectable, botComp.StandardMed, botComp.StandardMedAmount, out var accepted);
            _popup.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, target);
            _audio.PlayPvs(botComp.InjectSound, target);
            _chat.TrySendInGameICMessage(owner, Loc.GetString("medibot-finish-inject"), InGameICChatType.Speak, ChatTransmitRange.GhostRangeLimit);
            return HTNOperatorStatus.Finished;
        }

        return HTNOperatorStatus.Failed;
    }
}

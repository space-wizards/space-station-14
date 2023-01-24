using Content.Server.Chat.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.NPC.Components;
using Content.Server.Silicons.Bots;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed class MedibotInjectOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private ChatSystem _chat = default!;
    private SharedInteractionSystem _interactionSystem = default!;
    private SharedPopupSystem _popupSystem = default!;
    private SolutionContainerSystem _solutionSystem = default!;

    /// <summary>
    /// Target entity to inject.
    /// </summary>
    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _chat = sysManager.GetEntitySystem<ChatSystem>();
        _interactionSystem = sysManager.GetEntitySystem<SharedInteractionSystem>();
        _popupSystem = sysManager.GetEntitySystem<SharedPopupSystem>();
        _solutionSystem = sysManager.GetEntitySystem<SolutionContainerSystem>();
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

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) || _entManager.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (!_entManager.TryGetComponent<MedibotComponent>(owner, out var botComp))
            return HTNOperatorStatus.Failed;

        // To avoid spam, the rest of this needs fixing.
        _entManager.EnsureComponent<NPCRecentlyInjectedComponent>(target);

        if (!_entManager.TryGetComponent<DamageableComponent>(target, out var damage))
            return HTNOperatorStatus.Failed;

        if (!_solutionSystem.TryGetInjectableSolution(target, out var injectable))
            return HTNOperatorStatus.Failed;

        if (!_interactionSystem.InRangeUnobstructed(owner, target))
            return HTNOperatorStatus.Failed;

        if (damage.TotalDamage == 0)
            return HTNOperatorStatus.Failed;

        if (damage.TotalDamage >= MedibotComponent.EmergencyMedDamageThreshold)
        {
            _solutionSystem.TryAddReagent(target, injectable, botComp.EmergencyMed, botComp.EmergencyMedInjectAmount, out var accepted);
            _popupSystem.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, target);
            SoundSystem.Play("/Audio/Items/hypospray.ogg", Filter.Pvs(target), target);
            _chat.TrySendInGameICMessage(owner, Loc.GetString("medibot-finish-inject"), InGameICChatType.Speak, hideChat: false, hideGlobalGhostChat: true);
            return HTNOperatorStatus.Finished;
        }

        if (damage.TotalDamage >= MedibotComponent.StandardMedDamageThreshold)
        {
            _solutionSystem.TryAddReagent(target, injectable, botComp.StandardMed, botComp.StandardMedInjectAmount, out var accepted);
            _popupSystem.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, target);
            SoundSystem.Play("/Audio/Items/hypospray.ogg", Filter.Pvs(target), target);
            _chat.TrySendInGameICMessage(owner, Loc.GetString("medibot-finish-inject"), InGameICChatType.Speak, hideChat: false, hideGlobalGhostChat: true);
            return HTNOperatorStatus.Finished;
        }

        return HTNOperatorStatus.Failed;
    }
}

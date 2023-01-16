using System.Diagnostics.CodeAnalysis;
using Content.Shared.Alert;
using Content.Shared.Body.Systems;
using Content.Shared.Coordinates;
using Content.Shared.Medical.MedicalConditions.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.MedicalConditions.Systems;

public sealed partial class MedicalConditionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    private const string ConditionContainerId = "MedicalConditionsContainer";

    public override void Initialize()
    {
        SubscribeLocalEvent<MedicalConditionComponent, ComponentGetState>(OnGetConditionState);
        SubscribeLocalEvent<MedicalConditionComponent, ComponentHandleState>(OnHandleConditionState);
        SubscribeLocalEvent<MedicalConditionReceiverComponent, ComponentGetState>(OnGetReceiverState);
        SubscribeLocalEvent<MedicalConditionReceiverComponent, ComponentHandleState>(OnHandleReceiverState);
    }

    # region Public API

    public void AddCondition(EntityUid receiverEntity, string conditionName,
        MedicalConditionReceiverComponent? receiver = null)
    {
        if (!Resolve(receiverEntity, ref receiver))
            return;
        if (receiver.PreventedConditions != null && receiver.PreventedConditions.Contains(conditionName))
            return;
        if (!_containers.TryGetContainer(receiverEntity, ConditionContainerId, out var container))
        {
            container = _containers.EnsureContainer<Container>(receiverEntity, ConditionContainerId);
        }

        var newEntity = Spawn(conditionName, receiverEntity.ToCoordinates());
        var condition = EnsureComp<MedicalConditionComponent>(newEntity);
        if (receiver.PreventedConditionGroups != null && receiver.PreventedConditionGroups.Contains(condition.Group))
        {
            EntityManager.DeleteEntity(newEntity);
            return;
        }

        container.Insert(newEntity);
        var ev = new MedicalConditionAdded(receiverEntity, newEntity, condition, receiver);
        ConditionsUpdated(receiverEntity, newEntity, condition, receiver);
        RaiseLocalEvent(receiverEntity, ref ev);
    }

    public bool RemoveCondition(EntityUid receiverEntity, string conditionName,
        MedicalConditionReceiverComponent? receiver = null)
    {
        if (!Resolve(receiverEntity, ref receiver))
            return false;
        EntityUid? conditionEntity = null;
        MedicalConditionComponent? condition = null;
        foreach (var (foundConditionId, foundCondition) in GetAllConditions(receiverEntity))
        {
            if (Name(foundConditionId) != conditionName)
                continue;
            condition = foundCondition;
            conditionEntity = foundConditionId;
            break;
        }

        if (conditionEntity == null || condition == null)
            return false;
        _containers.RemoveEntity(receiverEntity, conditionEntity.Value);

        var ev = new MedicalConditionRemoved(receiverEntity, conditionEntity.Value, condition, receiver);
        ConditionsUpdated(receiverEntity, conditionEntity.Value, condition, receiver);
        RaiseLocalEvent(receiverEntity, ref ev);
        return true;
    }

    public bool TryGetAllConditionEntities(EntityUid receiverEntity,
        [NotNullWhen(true)] out IReadOnlyList<EntityUid>? conditions)
    {
        conditions = null;
        if (!HasComp<MedicalConditionReceiverComponent>(receiverEntity))
            return false;

        if (!_containers.TryGetContainer(receiverEntity, ConditionContainerId, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            return false;
        }

        conditions = container.ContainedEntities;
        return true;
    }

    public IEnumerable<EntityUid> GetAllConditionEntities(EntityUid receiverEntity)
    {
        if (!HasComp<MedicalConditionReceiverComponent>(receiverEntity))
            yield break;
        if (!_containers.TryGetContainer(receiverEntity, ConditionContainerId, out var container) ||
            container.ContainedEntities.Count == 0)
            yield break;
        foreach (var entityId in container.ContainedEntities)
        {
            yield return entityId;
        }
    }

    public IEnumerable<(EntityUid, MedicalConditionComponent)> GetAllConditions(EntityUid receiverEntity)
    {
        if (!HasComp<MedicalConditionReceiverComponent>(receiverEntity))
            yield break;
        if (!TryGetAllConditionEntities(receiverEntity, out var conditions))
            yield break;

        foreach (var conditionId in conditions)
        {
            if (TryComp<MedicalConditionComponent>(conditionId, out var condition))
                yield return (conditionId, condition);
        }
    }

    public bool TryGetCondition(EntityUid receiverEntity, string conditionName,
        [NotNullWhen(true)] out (EntityUid, MedicalConditionComponent)? conditionData,
        MedicalConditionReceiverComponent? receiver = null)
    {
        conditionData = null;
        if (!Resolve(receiverEntity, ref receiver) || !TryGetAllConditionEntities(receiverEntity, out var conditions))
            return false;

        foreach (var (foundConditionId, condition) in GetAllConditions(receiverEntity))
        {
            if (Name(foundConditionId) != conditionName)
                continue;
            conditionData = (foundConditionId, condition);
            return true;
        }

        return false;
    }

    #endregion

    #region Private Implementation

    private void OnGetReceiverState(EntityUid uid, MedicalConditionReceiverComponent component,
        ref ComponentGetState args)
    {
        args.State =
            new MedicalConditionReceiverComponentState(
                component.PreventedConditions,
                component.PreventedConditionGroups);
    }

    private void OnHandleReceiverState(EntityUid uid, MedicalConditionReceiverComponent component,
        ref ComponentHandleState args)
    {
        if (args.Current is not MedicalConditionReceiverComponentState state)
            return;
        component.PreventedConditions = state.PreventedConditions;
        component.PreventedConditionGroups = state.PreventedConditionGroups;
    }

    private void OnHandleConditionState(EntityUid uid, MedicalConditionComponent component,
        ref ComponentHandleState args)
    {
        if (args.Current is not MedicalConditionComponentState state)
            return;
        component.Alert = state.Alert;
        component.Group = state.Group;
        component.Description = state.Description;
        component.Severity = state.Severity;
    }

    private void OnGetConditionState(EntityUid uid, MedicalConditionComponent component, ref ComponentGetState args)
    {
        args.State = new MedicalConditionComponentState(
            component.Description,
            component.Group,
            component.Alert,
            component.Severity);
    }

    private void ConditionsUpdated(EntityUid receiverEntity, EntityUid conditionEntity,
        MedicalConditionComponent condition, MedicalConditionReceiverComponent receiver)
    {
        var ev2 = new MedicalConditionUpdated(receiverEntity, conditionEntity, condition, receiver);
        RaiseLocalEvent(receiverEntity, ref ev2, true);
        UpdateAlerts(receiverEntity, conditionEntity, condition, receiver);
    }

    #endregion
}

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Alert;
using Content.Shared.Coordinates;
using Content.Shared.Medical.Symptoms.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Symptoms.Systems;

public sealed partial class SymptomSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    private const string ConditionContainerId = "SymptomsContainer";

    public override void Initialize()
    {
        SubscribeLocalEvent<SymptomComponent, ComponentGetState>(OnGetConditionState);
        SubscribeLocalEvent<SymptomComponent, ComponentHandleState>(OnHandleConditionState);
        SubscribeLocalEvent<SymptomReceiverComponent, ComponentGetState>(OnGetReceiverState);
        SubscribeLocalEvent<SymptomReceiverComponent, ComponentHandleState>(OnHandleReceiverState);
    }

    # region Public API

    public void AddCondition(EntityUid receiverEntity, string conditionName,
        SymptomReceiverComponent? receiver = null)
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
        var condition = EnsureComp<SymptomComponent>(newEntity);

        if (receiver.PreventedConditionGroups != null && receiver.PreventedConditionGroups.Contains(condition.Group))
        {
            EntityManager.DeleteEntity(newEntity);
                return;
        }

        container.Insert(newEntity);
        var ev = new SymptomAdded(receiverEntity, newEntity, condition, receiver);
        ConditionsUpdated(receiverEntity, newEntity, condition, receiver);
        RaiseLocalEvent(receiverEntity, ref ev);
    }

    public bool RemoveCondition(EntityUid receiverEntity, string conditionName,
        SymptomReceiverComponent? receiver = null)
    {
        if (!Resolve(receiverEntity, ref receiver))
            return false;
        EntityUid? conditionEntity = null;
        SymptomComponent? condition = null;
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

        var ev = new SymptomRemoved(receiverEntity, conditionEntity.Value, condition, receiver);
        ConditionsUpdated(receiverEntity, conditionEntity.Value, condition, receiver);
        RaiseLocalEvent(receiverEntity, ref ev);
        return true;
    }

    public bool TryGetAllConditionEntities(EntityUid receiverEntity,
        [NotNullWhen(true)] out IReadOnlyList<EntityUid>? conditions)
    {
        conditions = null;
        if (!HasComp<SymptomReceiverComponent>(receiverEntity))
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
        if (!HasComp<SymptomReceiverComponent>(receiverEntity))
            yield break;
        if (!_containers.TryGetContainer(receiverEntity, ConditionContainerId, out var container) ||
            container.ContainedEntities.Count == 0)
            yield break;
        foreach (var entityId in container.ContainedEntities)
        {
            yield return entityId;
        }
    }

    public IEnumerable<(EntityUid, SymptomComponent)> GetAllConditions(EntityUid receiverEntity)
    {
        if (!HasComp<SymptomReceiverComponent>(receiverEntity))
            yield break;
        if (!TryGetAllConditionEntities(receiverEntity, out var conditions))
            yield break;

        foreach (var conditionId in conditions)
        {
            if (TryComp<SymptomComponent>(conditionId, out var condition))
                yield return (conditionId, condition);
        }
    }

    public bool TryGetCondition(EntityUid receiverEntity, string conditionName,
        [NotNullWhen(true)] out (EntityUid, SymptomComponent)? conditionData,
        SymptomReceiverComponent? receiver = null)
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

    private void OnGetReceiverState(EntityUid uid, SymptomReceiverComponent component,
        ref ComponentGetState args)
    {
        args.State =
            new SymptomReceiverComponentState(
                component.PreventedConditions,
                component.PreventedConditionGroups);
    }

    private void OnHandleReceiverState(EntityUid uid, SymptomReceiverComponent component,
        ref ComponentHandleState args)
    {
        if (args.Current is not SymptomReceiverComponentState state)
            return;
        component.PreventedConditions = state.PreventedConditions;
        component.PreventedConditionGroups = state.PreventedConditionGroups;
    }

    private void OnHandleConditionState(EntityUid uid, SymptomComponent component,
        ref ComponentHandleState args)
    {
        if (args.Current is not SymptomComponentState state)
            return;
        component.Alert = state.Alert;
        component.Group = state.Group;
        component.Description = state.Description;
        component.Severity = state.Severity;
    }

    private void OnGetConditionState(EntityUid uid, SymptomComponent component, ref ComponentGetState args)
    {
        args.State = new SymptomComponentState(
            component.Description,
            component.Group,
            component.Alert,
            component.Severity);
    }

    private void ConditionsUpdated(EntityUid receiverEntity, EntityUid conditionEntity,
        SymptomComponent condition, SymptomReceiverComponent receiver)
    {
        var ev2 = new SymptomUpdated(receiverEntity, conditionEntity, condition, receiver);
        RaiseLocalEvent(receiverEntity, ref ev2, true);
        UpdateAlerts(receiverEntity, conditionEntity, condition, receiver);
    }

    #endregion
}

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Alert;
using Content.Shared.Coordinates;
using Content.Shared.Medical.Symptoms.Components;
using Content.Shared.Medical.Symptoms.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Symptoms.Systems;

public sealed partial class SymptomSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    private const string SymptomContainerId = "SymptomsContainer";

    public override void Initialize()
    {
        SubscribeLocalEvent<SymptomComponent, ComponentGetState>(OnGetSymptomState);
        SubscribeLocalEvent<SymptomComponent, ComponentHandleState>(OnHandleSymptomState);
        SubscribeLocalEvent<SymptomReceiverComponent, ComponentGetState>(OnGetReceiverState);
        SubscribeLocalEvent<SymptomReceiverComponent, ComponentHandleState>(OnHandleReceiverState);
    }

    #region Public API
    public bool AddSymptom(EntityUid receiverEntity, string symptomName, SymptomReceiverComponent? receiver = null)
    {
        if (!Resolve(receiverEntity, ref receiver) ||
            receiver.SymptomGroup == null)
        {
            return false;
        }

        var symptomGroup = _prototypeManager.Index<SymptomGroupPrototype>(receiver.SymptomGroup);
        if (!symptomGroup.Symptoms.Contains(symptomName))
            return false;

        var container = _containers.EnsureContainer<Container>(receiverEntity, SymptomContainerId);
        var newEntity = Spawn(symptomName, receiverEntity.ToCoordinates());
        var symptom = EnsureComp<SymptomComponent>(newEntity);

        container.Insert(newEntity);
        SymptomsUpdated(receiverEntity, newEntity, symptom, receiver);

        var ev = new SymptomAdded(receiverEntity, newEntity, symptom, receiver);
        RaiseLocalEvent(receiverEntity, ref ev);
        return true;
    }

    public bool RemoveSymptom(EntityUid receiverEntity, string symptomName,
        SymptomReceiverComponent? receiver = null)
    {
        if (!Resolve(receiverEntity, ref receiver))
            return false;

        EntityUid? symptomEntity = null;
        SymptomComponent? symptom = null;

        foreach (var (foundId, found) in GetAllSymptoms(receiverEntity))
        {
            if (Name(foundId) != symptomName)
                continue;

            symptom = found;
            symptomEntity = foundId;
            break;
        }

        if (symptomEntity == null || symptom == null)
            return false;

        if (_netManager.IsServer)
            Del(symptomEntity.Value);

        SymptomsUpdated(receiverEntity, symptomEntity.Value, symptom, receiver);

        var ev = new SymptomRemoved(receiverEntity, symptomEntity.Value, symptom, receiver);
        RaiseLocalEvent(receiverEntity, ref ev);

        return true;
    }

    public bool TryGetAllSymptomEntities(
        EntityUid receiverEntity,
        [NotNullWhen(true)] out IReadOnlyList<EntityUid>? symptoms)
    {
        symptoms = null;

        if (!HasComp<SymptomReceiverComponent>(receiverEntity))
            return false;

        if (!_containers.TryGetContainer(receiverEntity, SymptomContainerId, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            return false;
        }

        symptoms = container.ContainedEntities;
        return true;
    }

    public IEnumerable<EntityUid> GetAllSymptomEntities(EntityUid receiverEntity)
    {
        if (!HasComp<SymptomReceiverComponent>(receiverEntity))
            yield break;

        if (!_containers.TryGetContainer(receiverEntity, SymptomContainerId, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            yield break;
        }

        foreach (var entityId in container.ContainedEntities)
        {
            yield return entityId;
        }
    }

    public IEnumerable<(EntityUid, SymptomComponent)> GetAllSymptoms(EntityUid receiverEntity)
    {
        if (!HasComp<SymptomReceiverComponent>(receiverEntity))
            yield break;

        if (!TryGetAllSymptomEntities(receiverEntity, out var symptoms))
            yield break;

        foreach (var symptomId in symptoms)
        {
            if (TryComp<SymptomComponent>(symptomId, out var symptom))
                yield return (symptomId, symptom);
        }
    }

    public bool TryGetSymptom(EntityUid receiverEntity, string symptomName,
        [NotNullWhen(true)] out (EntityUid, SymptomComponent)? symptomData,
        SymptomReceiverComponent? receiver = null)
    {
        symptomData = null;

        if (!Resolve(receiverEntity, ref receiver) ||
            !TryGetAllSymptomEntities(receiverEntity, out var symptoms))
            return false;

        foreach (var symptomId in symptoms)
        {
            if (Name(symptomId) != symptomName)
                continue;

            symptomData = (symptomId, Comp<SymptomComponent>(symptomId));
            return true;
        }

        return false;
    }

    #endregion

    #region Private Implementation

    private void OnGetReceiverState(EntityUid uid, SymptomReceiverComponent component,
        ref ComponentGetState args)
    {
        args.State = new SymptomReceiverComponentState(component.SymptomGroup);
    }

    private void OnHandleReceiverState(EntityUid uid, SymptomReceiverComponent component,
        ref ComponentHandleState args)
    {
        if (args.Current is not SymptomReceiverComponentState state)
            return;

        component.SymptomGroup = state.SymptomGroup;
    }

    private void OnHandleSymptomState(EntityUid uid, SymptomComponent component,
        ref ComponentHandleState args)
    {
        if (args.Current is not SymptomComponentState state)
            return;

        component.Alert = state.Alert;
        component.Group = state.Group;
        component.Description = state.Description;
        component.Severity = state.Severity;
    }

    private void OnGetSymptomState(EntityUid uid, SymptomComponent component, ref ComponentGetState args)
    {
        args.State = new SymptomComponentState(
            component.Description,
            component.Group,
            component.Alert,
            component.Severity
        );
    }

    private void SymptomsUpdated(EntityUid receiverEntity, EntityUid symptomEntity,
        SymptomComponent symptom, SymptomReceiverComponent receiver)
    {
        var ev = new SymptomUpdated(receiverEntity, symptomEntity, symptom, receiver);
        RaiseLocalEvent(receiverEntity, ref ev, true);
        UpdateAlerts(receiverEntity, symptomEntity, symptom);
    }
    #endregion
}

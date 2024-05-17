using Content.Server.Administration.Logs;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Payload.Components;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;
using System.Linq;
using Robust.Server.GameObjects;

namespace Content.Server.Payload.EntitySystems;

public sealed class PayloadSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PayloadCaseComponent, TriggerEvent>(OnCaseTriggered);
        SubscribeLocalEvent<PayloadTriggerComponent, TriggerEvent>(OnTriggerTriggered);
        SubscribeLocalEvent<PayloadCaseComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
        SubscribeLocalEvent<PayloadCaseComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);
        SubscribeLocalEvent<PayloadCaseComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ChemicalPayloadComponent, TriggerEvent>(HandleChemicalPayloadTrigger);
    }

    public IEnumerable<EntityUid> GetAllPayloads(EntityUid uid, ContainerManagerComponent? contMan = null)
    {
        if (!Resolve(uid, ref contMan, false))
            yield break;

        foreach (var container in contMan.Containers.Values)
        {
            foreach (var entity in container.ContainedEntities)
            {
                if (_tagSystem.HasTag(entity, "Payload"))
                    yield return entity;
            }
        }
    }

    private void OnCaseTriggered(EntityUid uid, PayloadCaseComponent component, TriggerEvent args)
    {
        if (!TryComp(uid, out ContainerManagerComponent? contMan))
            return;

        // Pass trigger event onto all contained payloads. Payload capacity configurable by construction graphs.
        foreach (var ent in GetAllPayloads(uid, contMan))
        {
            RaiseLocalEvent(ent, args, false);
        }
    }

    private void OnTriggerTriggered(EntityUid uid, PayloadTriggerComponent component, TriggerEvent args)
    {
        if (!component.Active)
            return;

        if (Transform(uid).ParentUid is not { Valid: true } parent)
            return;

        // Ensure we don't enter a trigger-loop
        DebugTools.Assert(!_tagSystem.HasTag(uid, "Payload"));

        RaiseLocalEvent(parent, args, false);
    }

    private void OnEntityInserted(EntityUid uid, PayloadCaseComponent _, EntInsertedIntoContainerMessage args)
    {
        if (!TryComp(args.Entity, out PayloadTriggerComponent? trigger))
            return;

        trigger.Active = true;

        if (trigger.Components == null)
            return;

        // ANY payload trigger that gets inserted can grant components. It is up to the construction graphs to determine trigger capacity.
        foreach (var (name, data) in trigger.Components)
        {
            if (!_componentFactory.TryGetRegistration(name, out var registration))
                continue;

            if (HasComp(uid, registration.Type))
                continue;

            if (_componentFactory.GetComponent(registration.Type) is not Component component)
                continue;

            var temp = (object) component;
            _serializationManager.CopyTo(data.Component, ref temp);
            EntityManager.AddComponent(uid, (Component) temp!);

            trigger.GrantedComponents.Add(registration.Type);
        }
    }

    private void OnEntityRemoved(EntityUid uid, PayloadCaseComponent component, EntRemovedFromContainerMessage args)
    {
        if (!TryComp(args.Entity, out PayloadTriggerComponent? trigger))
            return;

        trigger.Active = false;

        foreach (var type in trigger.GrantedComponents)
        {
            EntityManager.RemoveComponent(uid, type);
        }

        trigger.GrantedComponents.Clear();
    }

    private void OnExamined(EntityUid uid, PayloadCaseComponent component, ExaminedEvent args)
    {
        using (args.PushGroup(nameof(PayloadCaseComponent)))
        {
            if (!args.IsInDetailsRange)
            {
                args.PushMarkup(Loc.GetString("payload-case-not-close-enough", ("ent", uid)));
                return;
            }

            if (GetAllPayloads(uid).Any())
            {
                args.PushMarkup(Loc.GetString("payload-case-has-payload", ("ent", uid)));
            }
            else
            {
                args.PushMarkup(Loc.GetString("payload-case-does-not-have-payload", ("ent", uid)));
            }
        }
    }

    private void HandleChemicalPayloadTrigger(Entity<ChemicalPayloadComponent> entity, ref TriggerEvent args)
    {
        if (entity.Comp.BeakerSlotA.Item is not EntityUid beakerA
            || entity.Comp.BeakerSlotB.Item is not EntityUid beakerB
            || !TryComp(beakerA, out FitsInDispenserComponent? compA)
            || !TryComp(beakerB, out FitsInDispenserComponent? compB)
            || !_solutionContainerSystem.TryGetSolution(beakerA, compA.Solution, out var solnA, out var solutionA)
            || !_solutionContainerSystem.TryGetSolution(beakerB, compB.Solution, out var solnB, out var solutionB)
            || solutionA.Volume == 0
            || solutionB.Volume == 0)
        {
            return;
        }

        var solStringA = SolutionContainerSystem.ToPrettyString(solutionA);
        var solStringB = SolutionContainerSystem.ToPrettyString(solutionB);

        _adminLogger.Add(LogType.ChemicalReaction,
            $"Chemical bomb payload {ToPrettyString(entity.Owner):payload} at {_transform.GetMapCoordinates(entity.Owner):location} is combining two solutions: {solStringA:solutionA} and {solStringB:solutionB}");

        solutionA.MaxVolume += solutionB.MaxVolume;
        _solutionContainerSystem.TryAddSolution(solnA.Value, solutionB);
        _solutionContainerSystem.RemoveAllSolution(solnB.Value);

        // The grenade might be a dud. Redistribute solution:
        var tmpSol = _solutionContainerSystem.SplitSolution(solnA.Value, solutionA.Volume * solutionB.MaxVolume / solutionA.MaxVolume);
        _solutionContainerSystem.TryAddSolution(solnB.Value, tmpSol);
        solutionA.MaxVolume -= solutionB.MaxVolume;
        _solutionContainerSystem.UpdateChemicals(solnA.Value);

        args.Handled = true;
    }
}

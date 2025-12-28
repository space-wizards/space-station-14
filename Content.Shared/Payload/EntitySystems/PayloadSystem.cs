using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Payload.Components;
using Content.Shared.Tag;
using Content.Shared.Trigger;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Shared.Payload.EntitySystems;

/// <summary>
/// Handles activation, triggering, and explosions of payload entity.
/// </summary>
public sealed class PayloadSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    private static readonly ProtoId<TagPrototype> PayloadTag = "Payload";

    // TODO: Construction System Integration tests and remove the EnsureContainer from ConstructionSystem. :(
    private static readonly string PayloadContainer = "payload";
    private static readonly string TriggerContainer = "payloadTrigger";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PayloadCaseComponent, TriggerEvent>(OnCaseTriggered);
        SubscribeLocalEvent<PayloadTriggerComponent, TriggerEvent>(OnTriggerTriggered);
        SubscribeLocalEvent<PayloadCaseComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<PayloadCaseComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
        SubscribeLocalEvent<PayloadCaseComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);
        SubscribeLocalEvent<PayloadCaseComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ChemicalPayloadComponent, TriggerEvent>(HandleChemicalPayloadTrigger);
    }

    public IEnumerable<EntityUid> GetAllPayloads(EntityUid uid)
    {
        if (!_container.TryGetContainer(uid, PayloadContainer, out var container))
            yield break;

        foreach (var ent in container.ContainedEntities)
        {
            if (_tagSystem.HasTag(ent, PayloadTag))
                yield return ent;
        }
    }

    private void OnCaseTriggered(Entity<PayloadCaseComponent> ent, ref TriggerEvent args)
    {
        // TODO: Adjust to the new trigger system
        // Pass trigger event onto all contained payloads. Payload capacity configurable by construction graphs.
        foreach (var entAll in GetAllPayloads(ent.Owner))
            RaiseLocalEvent(entAll, ref args, false);
    }

    private void OnTriggerTriggered(Entity<PayloadTriggerComponent> ent, ref TriggerEvent args)
    {
        // TODO: Adjust to the new trigger system

        if (!ent.Comp.Active)
            return;

        if (Transform(ent.Owner).ParentUid is not { Valid: true } parent)
            return;

        // Ensure we don't enter a trigger-loop.
        DebugTools.Assert(!_tagSystem.HasTag(ent.Owner, PayloadTag));

        RaiseLocalEvent(parent, ref args);
    }

    private void OnInsertAttempt(Entity<PayloadCaseComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID == PayloadContainer && !_tagSystem.HasTag(args.EntityUid, PayloadTag))
            args.Cancel();

        if (args.Container.ID == TriggerContainer && !HasComp<PayloadTriggerComponent>(args.EntityUid))
            args.Cancel();
    }

    private void OnEntityInserted(Entity<PayloadCaseComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != TriggerContainer || !TryComp(args.Entity, out PayloadTriggerComponent? trigger))
            return;

        trigger.Active = true;

        if (trigger.Components == null)
            return;

        // ANY payload trigger that gets inserted can grant components. It is up to the construction graphs to determine trigger capacity.
        foreach (var (name, data) in trigger.Components)
        {
            if (!Factory.TryGetRegistration(name, out var registration))
                continue;

            if (HasComp(ent.Owner, registration.Type))
                continue;

            if (Factory.GetComponent(registration.Type) is not Component component)
                continue;

            var temp = (object)component;
            _serializationManager.CopyTo(data.Component, ref temp);
            AddComp(ent.Owner, (Component)temp!);

            trigger.GrantedComponents.Add(registration.Type);
        }
    }

    private void OnEntityRemoved(Entity<PayloadCaseComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != TriggerContainer || !TryComp(args.Entity, out PayloadTriggerComponent? trigger))
            return;

        trigger.Active = false;

        foreach (var type in trigger.GrantedComponents)
            RemComp(ent.Owner, type);

        trigger.GrantedComponents.Clear();
    }

    private void OnExamined(Entity<PayloadCaseComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(PayloadCaseComponent)))
        {
            if (!args.IsInDetailsRange)
            {
                args.PushMarkup(Loc.GetString("payload-case-not-close-enough", ("ent", ent.Owner)));
                return;
            }

            if (GetAllPayloads(ent.Owner).Any())
                args.PushMarkup(Loc.GetString("payload-case-has-payload", ("ent", ent.Owner)));
            else
                args.PushMarkup(Loc.GetString("payload-case-does-not-have-payload", ("ent", ent.Owner)));
        }
    }

    private void HandleChemicalPayloadTrigger(Entity<ChemicalPayloadComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        if (ent.Comp.BeakerSlotA.Item is not EntityUid beakerA
            || ent.Comp.BeakerSlotB.Item is not EntityUid beakerB
            || !TryComp(beakerA, out FitsInDispenserComponent? compA)
            || !TryComp(beakerB, out FitsInDispenserComponent? compB)
            || !_solutionContainer.TryGetSolution(beakerA, compA.Solution, out var solnA, out var solutionA)
            || !_solutionContainer.TryGetSolution(beakerB, compB.Solution, out var solnB, out var solutionB)
            || solutionA.Volume == 0
            || solutionB.Volume == 0)
        {
            return;
        }

        var solStringA = SharedSolutionContainerSystem.ToPrettyString(solutionA);
        var solStringB = SharedSolutionContainerSystem.ToPrettyString(solutionB);

        _adminLogger.Add(LogType.ChemicalReaction,
            $"Chemical bomb payload {ToPrettyString(ent.Owner):payload} at {_transform.GetMapCoordinates(ent.Owner):location} is combining two solutions: {solStringA:solutionA} and {solStringB:solutionB}");

        solutionA.MaxVolume += solutionB.MaxVolume;
        _solutionContainer.TryAddSolution(solnA.Value, solutionB);
        _solutionContainer.RemoveAllSolution(solnB.Value);

        // The grenade might be a dud. Redistribute solution:
        var tmpSol = _solutionContainer.SplitSolution(solnA.Value, solutionA.Volume * solutionB.MaxVolume / solutionA.MaxVolume);
        _solutionContainer.TryAddSolution(solnB.Value, tmpSol);
        solutionA.MaxVolume -= solutionB.MaxVolume;
        _solutionContainer.UpdateChemicals(solnA.Value);

        args.Handled = true;
    }
}

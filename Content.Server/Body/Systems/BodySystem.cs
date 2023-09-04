using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Server.Humanoid;
using Content.Server.Kitchen.Components;
using Content.Server.Mind;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Kitchen.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Random.Helpers;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Body.Systems;

public sealed class BodySystem : SharedBodySystem
{
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyPartComponent, ComponentStartup>(OnPartStartup);
        SubscribeLocalEvent<BodyComponent, ComponentStartup>(OnBodyStartup);
        SubscribeLocalEvent<BodyComponent, MoveInputEvent>(OnRelayMoveInput);
        SubscribeLocalEvent<BodyComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<BodyComponent, BeingMicrowavedEvent>(OnBeingMicrowaved);
    }

    private void OnPartStartup(EntityUid uid, BodyPartComponent component, ComponentStartup args)
    {
        // This inter-entity relationship makes be deeply uncomfortable because its probably going to re-encounter
        // all of the networking & startup ordering issues that containers and joints have.
        // TODO just use containers. Please.

        foreach (var slot in component.Children.Values)
        {
            DebugTools.Assert(slot.Parent == uid);
            if (slot.Child == null)
                continue;

            if (TryComp(slot.Child, out BodyPartComponent? child))
            {
                child.ParentSlot = slot;
                Dirty(slot.Child.Value, child);
                continue;
            }

            Log.Error($"Body part encountered missing limbs: {ToPrettyString(uid)}. Slot: {slot.Id}");
            slot.Child = null;
        }

        foreach (var slot in component.Organs.Values)
        {
            DebugTools.Assert(slot.Parent == uid);
            if (slot.Child == null)
                continue;

            if (TryComp(slot.Child, out OrganComponent? child))
            {
                child.ParentSlot = slot;
                Dirty(slot.Child.Value, child);
                continue;
            }

            Log.Error($"Body part encountered missing organ: {ToPrettyString(uid)}. Slot: {slot.Id}");
            slot.Child = null;
        }
    }

    private void OnBodyStartup(EntityUid uid, BodyComponent component, ComponentStartup args)
    {
        if (component.Root is not { } slot)
            return;

        DebugTools.Assert(slot.Parent == uid);
        if (slot.Child == null)
            return;

        if (!TryComp(slot.Child, out BodyPartComponent? child))
        {
            Log.Error($"Body part encountered missing limbs: {ToPrettyString(uid)}. Slot: {slot.Id}");
            slot.Child = null;
            return;
        }

        child.ParentSlot = slot;
        Dirty(slot.Child.Value, child);
    }

    private void OnRelayMoveInput(EntityUid uid, BodyComponent component, ref MoveInputEvent args)
    {
        if (_mobState.IsDead(uid) && _mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            mind.TimeOfDeath ??= _gameTiming.RealTime;
            _ticker.OnGhostAttempt(mindId, true, mind: mind);
        }
    }

    private void OnApplyMetabolicMultiplier(EntityUid uid, BodyComponent component,
        ApplyMetabolicMultiplierEvent args)
    {
        foreach (var organ in GetBodyOrgans(uid, component))
        {
            RaiseLocalEvent(organ.Id, args);
        }
    }

    private void OnBeingMicrowaved(EntityUid uid, BodyComponent component, BeingMicrowavedEvent args)
    {
        if (args.Handled)
            return;

        // Don't microwave animals, kids
        _transform.AttachToGridOrMap(uid);
        _appearance.SetData(args.Microwave, MicrowaveVisualState.Bloody, true);
        GibBody(uid, false, component);

        args.Handled = true;
    }

    public override bool AttachPart(
        EntityUid? partId,
        BodyPartSlot slot,
        [NotNullWhen(true)] BodyPartComponent? part = null)
    {
        if (!base.AttachPart(partId, slot, part))
            return false;

        if (part.Body is { } body &&
            TryComp<HumanoidAppearanceComponent>(body, out var humanoid))
        {
            var layer = part.ToHumanoidLayers();
            if (layer != null)
            {
                var layers = HumanoidVisualLayersExtension.Sublayers(layer.Value);
                _humanoidSystem.SetLayersVisibility(body, layers, true, true, humanoid);
            }
        }

        return true;
    }

    public override bool DropPart(EntityUid? partId, BodyPartComponent? part = null)
    {
        if (partId == null || !Resolve(partId.Value, ref part))
            return false;

        if (!base.DropPart(partId, part))
            return false;

        var oldBody = part.Body;
        if (oldBody == null || !TryComp<HumanoidAppearanceComponent>(oldBody, out var humanoid))
            return true;

        var layer = part.ToHumanoidLayers();
        if (layer == null)
            return true;

        var layers = HumanoidVisualLayersExtension.Sublayers(layer.Value);
        _humanoidSystem.SetLayersVisibility(oldBody.Value, layers, false, true, humanoid);
        return true;
    }

    public override HashSet<EntityUid> GibBody(EntityUid? bodyId, bool gibOrgans = false, BodyComponent? body = null, bool deleteItems = false)
    {
        if (bodyId == null || !Resolve(bodyId.Value, ref body, false))
            return new HashSet<EntityUid>();

        if (LifeStage(bodyId.Value) >= EntityLifeStage.Terminating || EntityManager.IsQueuedForDeletion(bodyId.Value))
            return new HashSet<EntityUid>();

        var xform = Transform(bodyId.Value);
        if (xform.MapUid == null)
            return new HashSet<EntityUid>();

        var gibs = base.GibBody(bodyId, gibOrgans, body, deleteItems);

        var coordinates = xform.Coordinates;
        var filter = Filter.Pvs(bodyId.Value, entityManager: EntityManager);
        var audio = AudioParams.Default.WithVariation(0.025f);

        _audio.Play(body.GibSound, filter, coordinates, true, audio);

        if (TryComp(bodyId, out ContainerManagerComponent? container))
        {
            foreach (var cont in container.GetAllContainers().ToArray())
            {
                foreach (var ent in cont.ContainedEntities.ToArray())
                {
                    if (deleteItems)
                    {
                        QueueDel(ent);
                    }
                    else
                    {
                        cont.Remove(ent, EntityManager, force: true);
                        Transform(ent).Coordinates = coordinates;
                        ent.RandomOffset(0.25f);
                    }
                }
            }
        }

        RaiseLocalEvent(bodyId.Value, new BeingGibbedEvent(gibs));
        QueueDel(bodyId.Value);

        return gibs;
    }
}

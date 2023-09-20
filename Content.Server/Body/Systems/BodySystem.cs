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
using Robust.Shared.Map;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, MoveInputEvent>(OnRelayMoveInput);
        SubscribeLocalEvent<BodyComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<BodyComponent, BeingMicrowavedEvent>(OnBeingMicrowaved);
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
        SharedTransform.AttachToGridOrMap(uid);
        _appearance.SetData(args.Microwave, MicrowaveVisualState.Bloody, true);
        GibBody(uid, false, component);

        args.Handled = true;
    }

    protected override bool InternalAttachPart(EntityUid? bodyId, EntityUid? parentId, EntityUid partId ,BodyPartComponent part,
        string slotName, ContainerSlot container)
    {
        if (!base.InternalAttachPart(bodyId, parentId, partId, part, slotName, container))
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

    protected override bool InternalDetachPart(EntityUid? bodyId, EntityUid partId, BodyPartComponent part, string slotName,
        ContainerSlot container, bool reparent,
        EntityCoordinates? coords)
    {
        if (!base.InternalDetachPart(bodyId, partId, part, slotName, container, reparent, coords))
            return false;

        if (bodyId == null
            || !TryComp<HumanoidAppearanceComponent>(bodyId, out var humanoid))
            return true;
        var layer = part.ToHumanoidLayers();
        if (layer == null)
            return true;
        var layers = HumanoidVisualLayersExtension.Sublayers(layer.Value);
        _humanoidSystem.SetLayersVisibility(bodyId.Value, layers, false, true, humanoid);

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

        HashSet<ContainerSlot> containers = new();
        foreach (var (partId,part) in GetBodyChildren(bodyId, body))
        {
            foreach (var (_,slotData) in part.Children)
            {
                containers.Add(slotData.Container);
            }
        }

        foreach (var container in containers)
        {
            if (container.ContainedEntity == null)
                continue;
            var entity = container.ContainedEntity.Value;
            if (deleteItems)
            {
                QueueDel(entity);
            }
            else
            {
                container.Remove(entity, EntityManager, force: true);
                SharedTransform.SetCoordinates(entity,coordinates);
                entity.RandomOffset(0.25f);
            }
        }
        RaiseLocalEvent(bodyId.Value, new BeingGibbedEvent(gibs));
        QueueDel(bodyId.Value);

        return gibs;
    }
}

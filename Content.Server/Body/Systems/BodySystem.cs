using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Server.Humanoid;
using Content.Server.Kitchen.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Kitchen.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio.Systems;

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
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, MoveInputEvent>(OnRelayMoveInput);
        SubscribeLocalEvent<BodyComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<BodyComponent, BeingMicrowavedEvent>(OnBeingMicrowaved);
    }

    private void OnRelayMoveInput(EntityUid uid, BodyComponent component, ref MoveInputEvent args)
    {
        // If they haven't actually moved then ignore it.
        if ((args.Component.HeldMoveButtons &
             (MoveButtons.Down | MoveButtons.Left | MoveButtons.Up | MoveButtons.Right)) == 0x0)
        {
            return;
        }

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

    protected override void AddPart(
        EntityUid bodyUid,
        EntityUid partUid,
        string slotId,
        BodyPartComponent component,
        BodyComponent? bodyComp = null)
    {
        // TODO: Predict this probably.
        base.AddPart(bodyUid, partUid, slotId, component, bodyComp);

        if (TryComp<HumanoidAppearanceComponent>(bodyUid, out var humanoid))
        {
            var layer = component.ToHumanoidLayers();
            if (layer != null)
            {
                var layers = HumanoidVisualLayersExtension.Sublayers(layer.Value);
                _humanoidSystem.SetLayersVisibility(bodyUid, layers, true, true, humanoid);
            }
        }
    }

    protected override void RemovePart(
        EntityUid bodyUid,
        EntityUid partUid,
        string slotId,
        BodyPartComponent component,
        BodyComponent? bodyComp = null)
    {
        base.RemovePart(bodyUid, partUid, slotId, component, bodyComp);

        if (!TryComp<HumanoidAppearanceComponent>(bodyUid, out var humanoid))
            return;

        var layer = component.ToHumanoidLayers();

        if (layer == null)
            return;

        var layers = HumanoidVisualLayersExtension.Sublayers(layer.Value);
        _humanoidSystem.SetLayersVisibility(bodyUid, layers, false, true, humanoid);
    }

    public override HashSet<EntityUid> GibBody(EntityUid bodyId, bool gibOrgans = false, BodyComponent? body = null, bool deleteItems = false, bool deleteBrain = false)
    {
        if (!Resolve(bodyId, ref body, false))
            return new HashSet<EntityUid>();

        if (TerminatingOrDeleted(bodyId) || EntityManager.IsQueuedForDeletion(bodyId))
            return new HashSet<EntityUid>();

        var xform = Transform(bodyId);
        if (xform.MapUid == null)
            return new HashSet<EntityUid>();

        var gibs = base.GibBody(bodyId, gibOrgans, body, deleteItems, deleteBrain);

        var coordinates = xform.Coordinates;
        var filter = Filter.Pvs(bodyId, entityManager: EntityManager);
        var audio = AudioParams.Default.WithVariation(0.025f);

        _audio.PlayStatic(body.GibSound, filter, coordinates, true, audio);

        foreach (var entity in gibs)
        {
            if (deleteItems)
            {
                if (!HasComp<BrainComponent>(entity) || deleteBrain)
                {
                    QueueDel(entity);
                }
            }
            else
            {
                SharedTransform.SetCoordinates(entity, coordinates.Offset(_random.NextVector2(.3f)));
            }
        }
        RaiseLocalEvent(bodyId, new BeingGibbedEvent(gibs));
        QueueDel(bodyId);

        return gibs;
    }
}

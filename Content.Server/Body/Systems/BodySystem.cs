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
using Content.Shared.Gibbing.Components;
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

    public override HashSet<EntityUid> GibBody(
        EntityUid bodyId,
        bool gibOrgans = false,
        BodyComponent? body = null ,
        bool launchGibs = true,
        Vector2? splatDirection = null,
        float splatModifier = 1,
        Angle splatCone = default,
        SoundSpecifier? gibSoundOverride = null
    )
    {
        if (!Resolve(bodyId, ref body, false))
            return new HashSet<EntityUid>();

        if (TerminatingOrDeleted(bodyId) || EntityManager.IsQueuedForDeletion(bodyId))
            return new HashSet<EntityUid>();

        var xform = Transform(bodyId);
        if (xform.MapUid == null)
            return new HashSet<EntityUid>();

        var gibs = base.GibBody(bodyId, gibOrgans, body, launchGibs: launchGibs,
            splatDirection: splatDirection, splatModifier: splatModifier, splatCone:splatCone);
        RaiseLocalEvent(bodyId, new BeingGibbedEvent(gibs));
        QueueDel(bodyId);

        return gibs;
    }
}

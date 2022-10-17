using System.Diagnostics.CodeAnalysis;
using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Server.Humanoid;
using Content.Server.Kitchen.Components;
using Content.Server.Mind.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Random.Helpers;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Body.Systems;

public sealed class BodySystem : SharedBodySystem
{
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [Dependency] private readonly HumanoidSystem _humanoidSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, MoveInputEvent>(OnRelayMoveInput);
        SubscribeLocalEvent<BodyComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<BodyComponent, BeingMicrowavedEvent>(OnBeingMicrowaved);
    }

    private void OnRelayMoveInput(EntityUid uid, BodyComponent component, ref MoveInputEvent args)
    {
        if (EntityManager.TryGetComponent<MobStateComponent>(uid, out var mobState) &&
            mobState.IsDead() &&
            EntityManager.TryGetComponent<MindComponent>(uid, out var mind) &&
            mind.HasMind)
        {
            if (!mind.Mind!.TimeOfDeath.HasValue)
            {
                mind.Mind.TimeOfDeath = _gameTiming.RealTime;
            }

            _ticker.OnGhostAttempt(mind.Mind!, true);
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
        Transform(uid).AttachToGridOrMap();
        Gib(uid, false, component);

        args.Handled = true;
    }

    public override bool AttachPart
    (EntityUid? partId,
        BodyPartSlot slot,
        [NotNullWhen(true)] BodyPartComponent? part = null)
    {
        if (!base.AttachPart(partId, slot, part))
            return false;

        if (TryGetPartBody(partId, out var body, part) &&
            TryComp<HumanoidComponent>(body.Value.Id, out var humanoid))
        {
            var layer = part.ToHumanoidLayers();
            if (layer != null)
            {
                var layers = HumanoidVisualLayersExtension.Sublayers(layer.Value);
                _humanoidSystem.SetLayersVisibility(body.Value.Id, layers, true, true, humanoid);
            }
        }

        return true;
    }

    public override bool DropPart(EntityUid? partId, BodyPartComponent? part = null)
    {
        var body = GetPartBody(partId, part);

        if (!base.DropPart(partId, part))
            return false;

        if (body != null && TryComp<HumanoidComponent>(body.Value.Id, out var humanoid))
        {
            var layer = part.ToHumanoidLayers();
            if (layer != null)
            {
                var layers = HumanoidVisualLayersExtension.Sublayers(layer.Value);
                _humanoidSystem.SetLayersVisibility(body.Value.Id, layers, false, true, humanoid);
            }
        }

        return true;
    }

    public override HashSet<EntityUid> Gib(EntityUid? bodyId, bool gibOrgans = false, BodyComponent? body = null)
    {
        if (bodyId == null || !Resolve(bodyId.Value, ref body, false))
            return new HashSet<EntityUid>();

        var gibs = base.Gib(bodyId, gibOrgans, body);

        var xform = Transform(bodyId.Value);
        var coordinates = xform.Coordinates;
        var filter = Filter.Pvs(bodyId.Value, entityManager: EntityManager);
        var audio = AudioParams.Default.WithVariation(0.025f);

        _audio.Play(body.GibSound, filter, coordinates, audio);

        if (TryComp(bodyId, out ContainerManagerComponent? container))
        {
            foreach (var cont in container.GetAllContainers())
            {
                foreach (var ent in cont.ContainedEntities)
                {
                    cont.ForceRemove(ent);
                    Transform(ent).Coordinates = coordinates;
                    ent.RandomOffset(0.25f);
                }
            }
        }

        RaiseLocalEvent(bodyId.Value, new BeingGibbedEvent(gibs));
        QueueDel(bodyId.Value);

        return gibs;
    }
}

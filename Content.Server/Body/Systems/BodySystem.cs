using System.Linq;
using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Server.Mind.Components;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems.Body;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.EntitySystems;
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
    [Dependency] private readonly BodyPartSystem _bodyPartSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, RelayMoveInputEvent>(OnRelayMoveInput);
        SubscribeLocalEvent<BodyComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<BodyComponent, PartRemovedFromBodyEvent>(OnPartRemoved);

        SubscribeLocalEvent<FallDownNoLegsComponent, ComponentInit>(OnFallDownInit);
        SubscribeLocalEvent<FallDownNoLegsComponent, PartRemovedFromBodyEvent>(OnFallDownPartRemoved);
    }

    #region Overrides

    protected override void OnComponentInit(EntityUid uid, SharedBodyComponent component, ComponentInit args)
    {
        base.OnComponentInit(uid, component, args);

        var preset = _prototypeManager.Index<BodyPresetPrototype>(component.PresetId);
        foreach (var slot in component.SlotIds.Values)
        {
            // Using MapPosition instead of Coordinates here prevents
            // a crash within the character preview menu in the lobby
            var entity = Spawn(preset.PartIDs[slot.Id], Transform(uid).MapPosition);

            if (!TryComp(entity, out SharedBodyPartComponent? part))
            {
                Logger.Error($"Entity {slot.Id} does not have a {nameof(SharedBodyPartComponent)} component.");
                continue;
            }

            AddPart(uid, slot.Id, part, component);
        }
    }

    #endregion

    #region Body events

    private void OnComponentStartup(EntityUid uid, BodyComponent component, ComponentStartup args)
    {
        // This is ran in Startup as entities spawned in Initialize
        // are not synced to the client since they are assumed to be
        // identical on it

        // TODO that should probably get sussed out given we're removing startup eventually.
        foreach (var (part, _) in component.Parts)
        {
            part.Dirty();
        }
    }

    private void OnRelayMoveInput(EntityUid uid, BodyComponent component, RelayMoveInputEvent args)
    {
        if (EntityManager.TryGetComponent<MobStateComponent>(uid, out var mobState) &&
            mobState.IsDead() &&
            EntityManager.TryGetComponent<MindComponent>(uid, out var mind) &&
            mind.HasMind)
        {
            if (!mind.Mind!.TimeOfDeath.HasValue) mind.Mind.TimeOfDeath = _gameTiming.RealTime;

            _ticker.OnGhostAttempt(mind.Mind!, true);
        }
    }

    private void OnPartRemoved(EntityUid uid, BodyComponent component, PartRemovedFromBodyEvent args)
    {
        args.Part.Owner.RandomOffset(0.25f);
    }

    #endregion

    #region Fall down events

    private void OnFallDownPartRemoved(EntityUid uid, FallDownNoLegsComponent component, PartRemovedFromBodyEvent args)
    {
        if (!TryComp<SharedBodyComponent>(uid, out var body))
            return;

        if (args.Part.PartType == BodyPartType.Leg &&
            GetPartsOfType(uid, BodyPartType.Leg, body).ToArray().Length == 0)
        {
            _standingStateSystem.Down(uid);
        }
    }

    private void OnFallDownInit(EntityUid uid, FallDownNoLegsComponent component, ComponentInit args)
    {
        if (!TryComp<SharedBodyComponent>(uid, out var body))
            return;

        // if you spawn with no legs, then..
        if (GetPartsOfType(uid, BodyPartType.Leg, body).ToArray().Length == 0)
        {
            _standingStateSystem.Down(uid);
        }
    }

    #endregion

    public void Gib(EntityUid uid, bool gibParts = false,
        BodyComponent? body = null)
    {
        if (!Resolve(uid, ref body, false))
            return;

        foreach (var part in body.Parts.Keys)
        {
            RemovePart(uid, part, body);

            if (gibParts)
                _bodyPartSystem.Gib(uid, part);
        }

        SoundSystem.Play(Filter.Pvs(uid), body.GibSound.GetSound(), Transform(uid).Coordinates,
            AudioHelpers.WithVariation(0.025f));

        if (TryComp(uid, out ContainerManagerComponent? container))
        {
            foreach (var cont in container.GetAllContainers())
            {
                foreach (var ent in cont.ContainedEntities)
                {
                    cont.ForceRemove(ent);
                    Transform(ent).Coordinates = Transform(uid).Coordinates;
                    ent.RandomOffset(0.25f);
                }
            }
        }

        QueueDel(uid);
    }
}

using System.Linq;
using Content.Server.Climbing;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Bed.Sleep;
using Content.Shared.Destructible;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server._FTL.Cryopod;

public sealed class SleeperCryopodSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SleeperCryopodComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SleeperCryopodComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<SleeperCryopodComponent, SuicideEvent>(OnSuicide);
        SubscribeLocalEvent<SleeperCryopodComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SleeperCryopodComponent, DestructionEventArgs>((e,c,_) => EjectBody(e, c));
        SubscribeLocalEvent<SleeperCryopodComponent, DragDropDraggedEvent>(OnDragDrop);
        SubscribeLocalEvent<PlayerSpawningEvent>(OnSpawning, before: new [] {typeof(SpawnPointSystem)});
    }

    private void OnInit(EntityUid uid, SleeperCryopodComponent component, ComponentInit args)
    {
        component.BodyContainer = _container.EnsureContainer<ContainerSlot>(uid, "body_container");
    }

    private void OnSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        var validPods = EntityQuery<SleeperCryopodComponent>().Where(c => !IsOccupied(c)).ToArray();
        _random.Shuffle(validPods);
        if (!validPods.Any())
            return;

        var pod = validPods.First();
        var xform = Transform(pod.Owner);

        args.SpawnResult = _stationSpawning.SpawnPlayerMob(xform.Coordinates, args.Job, args.HumanoidCharacterProfile, args.Station);

        _audio.PlayPvs(pod.ArrivalSound, pod.Owner);
        InsertBody(args.SpawnResult.Value, pod);
        var duration = _random.NextFloat(pod.InitialSleepDurationRange.X, pod.InitialSleepDurationRange.Y);
        _statusEffects.TryAddStatusEffect<SleepingComponent>(args.SpawnResult.Value, "ForcedSleep", TimeSpan.FromSeconds(duration), false);
    }

    private void OnDragDrop(EntityUid uid, SleeperCryopodComponent component, ref DragDropDraggedEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = InsertBody(args.Target, component);
    }

    private void AddAlternativeVerbs(EntityUid uid, SleeperCryopodComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Eject verb
        if (IsOccupied(component))
        {
            AlternativeVerb verb = new()
            {
                Act = () => EjectBody(uid, component),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("medical-scanner-verb-noun-occupant")
            };
            args.Verbs.Add(verb);
        }

        // Self-insert verb
        if (!IsOccupied(component) &&
            _actionBlocker.CanMove(args.User))
        {
            AlternativeVerb verb = new()
            {
                Act = () => InsertBody(args.User, component),
                Category = VerbCategory.Insert,
                Text = Loc.GetString("medical-scanner-verb-enter")
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnSuicide(EntityUid uid, SleeperCryopodComponent component, SuicideEvent args)
    {
        if (args.Handled)
            return;

        if (args.Victim != component.BodyContainer.ContainedEntity)
            return;

        EntityManager.DeleteEntity(args.Victim);
        _audio.PlayPvs(component.LeaveSound, uid);
        args.SetHandled(SuicideKind.Special);
    }

    private void OnExamine(EntityUid uid, SleeperCryopodComponent component, ExaminedEvent args)
    {
        var message = component.BodyContainer.ContainedEntity == null
            ? "cryopod-examine-empty"
            : "cryopod-examine-occupied";

        args.PushMarkup(Loc.GetString(message));
    }

    public bool InsertBody(EntityUid? toInsert, SleeperCryopodComponent component)
    {
        if (toInsert == null)
            return false;

        if (IsOccupied(component))
            return false;

        if (!HasComp<MobStateComponent>(toInsert.Value))
            return false;

        return component.BodyContainer.Insert(toInsert.Value, EntityManager);
    }

    public bool EjectBody(EntityUid pod, SleeperCryopodComponent component)
    {
        if (!IsOccupied(component))
            return false;

        var toEject = component.BodyContainer.ContainedEntity;
        if (toEject == null)
            return false;

        component.BodyContainer.Remove(toEject.Value);
        _climb.ForciblySetClimbing(toEject.Value, pod);

        return true;
    }

    public bool IsOccupied(SleeperCryopodComponent component)
    {
        return component.BodyContainer.ContainedEntity != null;
    }
}

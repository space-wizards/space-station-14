using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Audio.Systems;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Pulling.Components;

namespace Content.Server.Starlight.Antags.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly PullingSystem _pullingSystem = default!;

    private static readonly EntProtoId<InstantActionComponent> _sendYourself = "ActionSendYourself";
    private static readonly EntProtoId<InstantActionComponent> _exitAction = "ActionExitConsole";

    private static readonly EntProtoId _teleportationEffect = "EffectTeleportation";
    private static readonly EntProtoId _teleportationEffectEntity = "EffectTeleportationEntity";
    public void InitializeActions()
    {
        SubscribeLocalEvent<AbductorScientistComponent, ComponentStartup>(AbductorScientistComponentStartup);

        SubscribeLocalEvent<ExitConsoleEvent>(OnExit);

        SubscribeLocalEvent<AbductorReturnToShipEvent>(OnReturn);
        SubscribeLocalEvent<AbductorScientistComponent, AbductorReturnDoAfterEvent>(OnDoAfterAbductorReturn);

        SubscribeLocalEvent<SendYourselfEvent>(OnSendYourself);
        SubscribeLocalEvent<AbductorScientistComponent, AbductorSendYourselfDoAfterEvent>(OnDoAfterSendYourself);
    }

    private void AbductorScientistComponentStartup(Entity<AbductorScientistComponent> ent, ref ComponentStartup args)
        => ent.Comp.SpawnPosition = EnsureComp<TransformComponent>(ent).Coordinates;

    private void OnReturn(AbductorReturnToShipEvent ev)
    {
        EnsureComp<AbductorScientistComponent>(ev.Performer, out var comp);

        _color.RaiseEffect(Color.FromHex("#BA0099"), new List<EntityUid>(1) { ev.Performer }, Filter.Pvs(ev.Performer, entityManager: EntityManager));
        EnsureComp<TransformComponent>(ev.Performer, out var xform);
        var effectEnt = SpawnAttachedTo(_teleportationEffectEntity, xform.Coordinates);
        _xformSys.SetParent(effectEnt, ev.Performer);
        EnsureComp<TimedDespawnComponent>(effectEnt, out var despawnEffectEntComp);
        despawnEffectEntComp.Lifetime = 3.0f;
        _audioSystem.PlayPvs("/Audio/_Starlight/Misc/alien_teleport.ogg", effectEnt);

        if (TryComp<AbductorScientistComponent>(ev.Performer, out var abductorScientistComponent) && abductorScientistComponent.SpawnPosition.HasValue)
        {
            var effect = _entityManager.SpawnEntity(_teleportationEffect, abductorScientistComponent.SpawnPosition.Value);
            EnsureComp<TimedDespawnComponent>(effect, out var despawnComp);
            despawnComp.Lifetime = 3.0f;
            _audioSystem.PlayPvs("/Audio/_Starlight/Misc/alien_teleport.ogg", effect);
        }

        var doAfter = new DoAfterArgs(EntityManager, ev.Performer, TimeSpan.FromSeconds(3), new AbductorReturnDoAfterEvent(), ev.Performer);
        _doAfter.TryStartDoAfter(doAfter);
        ev.Handled = true;
    }
    private void OnDoAfterAbductorReturn(Entity<AbductorScientistComponent> ent, ref AbductorReturnDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;
        
        _color.RaiseEffect(Color.FromHex("#BA0099"), new List<EntityUid>(1) { ent }, Filter.Pvs(ent, entityManager: EntityManager));
        if (_pullingSystem.IsPulling(ent))
        {
            if (!TryComp<PullerComponent>(ent, out var pullerComp)
                || pullerComp.Pulling == null
                || !TryComp<PullableComponent>(pullerComp.Pulling.Value, out var pullableComp)
                || !_pullingSystem.TryStopPull(pullerComp.Pulling.Value, pullableComp)) return;
        }

        if (_pullingSystem.IsPulled(ent))
        {
            if (!TryComp<PullableComponent>(ent, out var pullableComp)
                || !_pullingSystem.TryStopPull(ent, pullableComp)) return;
        }

        if (ent.Comp.SpawnPosition is not null)
            _xformSys.SetCoordinates(ent, ent.Comp.SpawnPosition.Value);
        OnCameraExit(ent);
    }

    private void OnSendYourself(SendYourselfEvent ev)
    {
        _color.RaiseEffect(Color.FromHex("#BA0099"), new List<EntityUid>(1) { ev.Performer }, Filter.Pvs(ev.Performer, entityManager: EntityManager));
        EnsureComp<TransformComponent>(ev.Performer, out var xform);
        var effectEnt = SpawnAttachedTo(_teleportationEffectEntity, xform.Coordinates);
        _xformSys.SetParent(effectEnt, ev.Performer);
        EnsureComp<TimedDespawnComponent>(effectEnt, out var despawnEffectEntComp);

        var effect = _entityManager.SpawnEntity(_teleportationEffect, ev.Target);
        EnsureComp<TimedDespawnComponent>(effect, out var despawnComp);

        var @event = new AbductorSendYourselfDoAfterEvent(GetNetCoordinates(ev.Target));
        var doAfter = new DoAfterArgs(EntityManager, ev.Performer, TimeSpan.FromSeconds(5), @event, ev.Performer);
        _doAfter.TryStartDoAfter(doAfter);
        ev.Handled = true;
    }
    private void OnDoAfterSendYourself(Entity<AbductorScientistComponent> ent, ref AbductorSendYourselfDoAfterEvent args)
    {
        _color.RaiseEffect(Color.FromHex("#BA0099"), new List<EntityUid>(1) { ent }, Filter.Pvs(ent, entityManager: EntityManager));
        if (_pullingSystem.IsPulling(ent))
        {
            if (!TryComp<PullerComponent>(ent, out var pullerComp)
                || pullerComp.Pulling == null
                || !TryComp<PullableComponent>(pullerComp.Pulling.Value, out var pullableComp)
                || !_pullingSystem.TryStopPull(pullerComp.Pulling.Value, pullableComp)) return;
        }

        if (_pullingSystem.IsPulled(ent))
        {
            if (!TryComp<PullableComponent>(ent, out var pullableComp)
                || !_pullingSystem.TryStopPull(ent, pullableComp)) return;
        }
        _xformSys.SetCoordinates(ent, GetCoordinates(args.TargetCoordinates));
        OnCameraExit(ent);
    }

    private void OnExit(ExitConsoleEvent ev) => OnCameraExit(ev.Performer);

    private void AddActions(AbductorBeaconChosenBuiMsg args)
    {
        EnsureComp<AbductorsAbilitiesComponent>(args.Actor, out var comp);
        comp.HiddenActions = _actions.HideActions(args.Actor);
        _actions.AddAction(args.Actor, ref comp.ExitConsole, _exitAction);
        _actions.AddAction(args.Actor, ref comp.SendYourself, _sendYourself);
    }
    private void RemoveActions(EntityUid actor)
    {
        EnsureComp<AbductorsAbilitiesComponent>(actor, out var comp);
        if (comp.ExitConsole is not null)
            _actions.RemoveAction(actor, comp.ExitConsole);
        if (comp.SendYourself is not null)
            _actions.RemoveAction(actor, comp.SendYourself);

        _actions.UnHideActions(actor, comp.HiddenActions);
    }
}

using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Server.GameObjects;

namespace Content.Server.Starlight.Antags.Abductor;

public sealed partial class ConsoleSystem : SharedAbductorSystem
{
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private readonly EntProtoId<InstantActionComponent> SendYourself = "ActionSendYourself";
    private readonly EntProtoId<InstantActionComponent> ExitAction = "ActionExitConsole";
    private readonly EntProtoId TeleportationEffect = "EffectTeleportation";
    private readonly EntProtoId TeleportationEffectEntity = "EffectTeleportationEntity";
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
        => ent.Comp.Position = _xformSys.GetMapCoordinates(ent);

    private void OnReturn(AbductorReturnToShipEvent ev)
    {
        EnsureComp<AbductorScientistComponent>(ev.Performer, out var comp);

        _color.RaiseEffect(Color.FromHex("#BA0099"), new List<EntityUid>(1) { ev.Performer }, Filter.Pvs(ev.Performer, entityManager: EntityManager));
        EnsureComp<TransformComponent>(ev.Performer, out var xform);
        var effectEnt = SpawnAttachedTo(TeleportationEffectEntity, xform.Coordinates);
        _transform.SetParent(effectEnt, ev.Performer);
        EnsureComp<TimedDespawnComponent>(effectEnt, out var despawnEffectEntComp);
        despawnEffectEntComp.Lifetime = 3.0f;

        if(TryComp<AbductorScientistComponent>(ev.Performer, out var abductorScientistComponent) && abductorScientistComponent.Position.HasValue)
        {
            var effect = _entityManager.SpawnEntity(TeleportationEffect,  abductorScientistComponent.Position.Value);
            EnsureComp<TimedDespawnComponent>(effect, out var despawnComp);
            despawnComp.Lifetime = 3.0f;
        }

        var doAfter = new DoAfterArgs(EntityManager, ev.Performer, TimeSpan.FromSeconds(3), new AbductorReturnDoAfterEvent(), ev.Performer);
        _doAfter.TryStartDoAfter(doAfter);
        ev.Handled = true;
    }
    private void OnDoAfterAbductorReturn(Entity<AbductorScientistComponent> ent, ref AbductorReturnDoAfterEvent args)
    {
        _color.RaiseEffect(Color.FromHex("#BA0099"), new List<EntityUid>(1) { ent }, Filter.Pvs(ent, entityManager: EntityManager));
        if (ent.Comp.Position is not null)
            _xformSys.SetMapCoordinates(ent, ent.Comp.Position.Value);
        OnCameraExit(ent);
    }

    private void OnSendYourself(SendYourselfEvent ev)
    {
        _color.RaiseEffect(Color.FromHex("#BA0099"), new List<EntityUid>(1) { ev.Performer }, Filter.Pvs(ev.Performer, entityManager: EntityManager));
        EnsureComp<TransformComponent>(ev.Performer, out var xform);
        var effectEnt = SpawnAttachedTo(TeleportationEffectEntity, xform.Coordinates);
        _transform.SetParent(effectEnt, ev.Performer);
        EnsureComp<TimedDespawnComponent>(effectEnt, out var despawnEffectEntComp);

        var effect = _entityManager.SpawnEntity(TeleportationEffect, ev.Target);
        EnsureComp<TimedDespawnComponent>(effect, out var despawnComp);

        var @event = new AbductorSendYourselfDoAfterEvent(GetNetCoordinates(ev.Target));
        var doAfter = new DoAfterArgs(EntityManager, ev.Performer, TimeSpan.FromSeconds(5), @event, ev.Performer);
        _doAfter.TryStartDoAfter(doAfter);
        ev.Handled = true;
    }
    private void OnDoAfterSendYourself(Entity<AbductorScientistComponent> ent, ref AbductorSendYourselfDoAfterEvent args)
    {
        _color.RaiseEffect(Color.FromHex("#BA0099"), new List<EntityUid>(1) { ent }, Filter.Pvs(ent, entityManager: EntityManager));
        if (ent.Comp.Position is not null)
            _xformSys.SetMapCoordinates(ent, _xformSys.ToMapCoordinates(args.TargetCoordinates));
        OnCameraExit(ent);
    }

    private void OnExit(ExitConsoleEvent ev) => OnCameraExit(ev.Performer);

    private void AddActions(AbductorBeaconChosenBuiMsg args)
    {
        EnsureComp<AbductorsAbilitiesComponent>(args.Actor, out var comp);
        comp.HiddenActions = _actions.HideActions(args.Actor);
        _actions.AddAction(args.Actor, ref comp.ExitConsole, ExitAction);
        _actions.AddAction(args.Actor, ref comp.SendYourself, SendYourself);
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

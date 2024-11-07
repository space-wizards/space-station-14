using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Starlight.Antags.Abductor;

public sealed partial class ConsoleSystem : SharedAbductorSystem
{
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;

    private readonly EntProtoId<InstantActionComponent> ExitAction = "ActionExitConsole";
    public void InitializeActions()
    {
        SubscribeLocalEvent<AbductorScientistComponent, ComponentStartup>(AbductorScientistComponentStartup);

        SubscribeLocalEvent<ExitConsoleEvent>(OnExit);
        SubscribeLocalEvent<AbductorReturnToShipEvent>(OnReturn);
        SubscribeLocalEvent<AbductorScientistComponent, AbductorReturnDoAfterEvent>(OnAbductorReturn);
    }

    private void OnAbductorReturn(Entity<AbductorScientistComponent> ent, ref AbductorReturnDoAfterEvent args)
    {
        _color.RaiseEffect(Color.FromHex("#BA0099"), new List<EntityUid>(1) { ent }, Filter.Pvs(ent, entityManager: EntityManager));
        if (ent.Comp.Position is not null)
            _xformSys.SetMapCoordinates(ent, ent.Comp.Position.Value);
    }
    private void AbductorScientistComponentStartup(Entity<AbductorScientistComponent> ent, ref ComponentStartup args)
        => ent.Comp.Position = _xformSys.GetMapCoordinates(ent);

    private void OnReturn(AbductorReturnToShipEvent ev)
    {
        EnsureComp<AbductorScientistComponent>(ev.Performer, out var comp);

        _color.RaiseEffect(Color.FromHex("#BA0099"), new List<EntityUid>(1) { ev.Performer }, Filter.Pvs(ev.Performer, entityManager: EntityManager));

        var doAfter = new DoAfterArgs(EntityManager, ev.Performer, TimeSpan.FromSeconds(3), new AbductorReturnDoAfterEvent(), ev.Performer);
        _doAfter.TryStartDoAfter(doAfter);
        ev.Handled = true;
    }
    private void OnExit(ExitConsoleEvent ev) => OnCameraExit(ev.Performer);

    private void AddActions(AbductorBeaconChosenBuiMsg args)
    {
        EnsureComp<AbductorsAbilitiesComponent>(args.Actor, out var comp);
        comp.HiddenActions = _actions.HideActions(args.Actor);
        _actions.AddAction(args.Actor, ref comp.ExitConsole, ExitAction);
    }
    private void RemoveActions(EntityUid actor)
    {
        EnsureComp<AbductorsAbilitiesComponent>(actor, out var comp);
        if (comp.ExitConsole is not null)
            _actions.RemoveAction(actor, comp.ExitConsole);

        _actions.UnHideActions(actor, comp.HiddenActions);
    }
}

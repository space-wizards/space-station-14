using Content.Shared.Actions;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed partial class ActionGunSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionGunComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ActionGunComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActionGunComponent, ActionGunShootEvent>(OnShoot);
    }

    private void OnMapInit(Entity<ActionGunComponent> ent, ref MapInitEvent args)
    {
        if (string.IsNullOrEmpty(ent.Comp.Action))
            return;

        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
        ent.Comp.Gun = Spawn(ent.Comp.GunProto);
    }

    private void OnShutdown(Entity<ActionGunComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Gun is { } gun)
            QueueDel(gun);
    }

    private void OnShoot(Entity<ActionGunComponent> ent, ref ActionGunShootEvent args)
    {
        if (!TryComp<GunComponent>(ent.Comp.Gun, out var gun))
            return;

        args.Handled = _gun.AttemptShoot(ent, (ent.Comp.Gun.Value, gun), args.Target);
    }

    /// <summary>
    /// Grants or revokes the shoot action at runtime, for abilities that are locked behind
    /// some condition (e.g. an upgrade) while keeping the component itself on the entity.
    /// </summary>
    public void SetActionGranted(Entity<ActionGunComponent> ent, bool granted)
    {
        if (granted == (ent.Comp.ActionEntity != null))
            return;

        if (granted)
        {
            _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
        }
        else
        {
            _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
            ent.Comp.ActionEntity = null;
        }
    }
}


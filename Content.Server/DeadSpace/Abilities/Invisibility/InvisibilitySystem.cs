using Content.Shared.Actions;
using Content.Shared.DeadSpace.Abilities.Invisibility;
using Content.Shared.DeadSpace.Abilities.Invisibility.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.DeadSpace.Abilities.Invisibility;

public sealed partial class InvisibilitySystem : SharedInvisibilitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InvisibilityComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<InvisibilityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<InvisibilityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<InvisibilityComponent, InvisibilityActionEvent>(DoInvisibility);
    }

    private void OnComponentInit(EntityUid uid, InvisibilityComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionInvisibilityEntity, component.ActionInvisibility, uid);
    }

    private void OnShutdown(EntityUid uid, InvisibilityComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionInvisibilityEntity);

        if (component.IsInvisible)
            TogleInvisibility(uid, component);
    }

    public void DoInvisibility(EntityUid uid, InvisibilityComponent component, InvisibilityActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        TogleInvisibility(uid, component);
    }

    private void OnMeleeHit(EntityUid uid, InvisibilityComponent component, MeleeHitEvent args)
    {
        if (!component.IsInvisible)
            return;

        TogleInvisibility(uid, component);
    }

}

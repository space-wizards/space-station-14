using Content.Server.Magic.Events;
using Content.Server.Projectiles.Components;
using Content.Server.Weapon.Ranged;
using Content.Server.Wieldable;
using Content.Server.Wieldable.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Magic;

public sealed class SpellSystem : EntitySystem
{
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpellbookComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpellbookComponent, ItemWieldedEvent>(OnWielded);
        SubscribeLocalEvent<SpellbookComponent, ItemUnwieldedEvent>(OnUnwielded);
        SubscribeLocalEvent<SpellbookComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<ReagentEffectSpellEvent>(OnReagentSpell);
        SubscribeLocalEvent<ProjectileSpellEvent>(OnProjectileSpell);
        SubscribeLocalEvent<TeleportSpellEvent>(OnTeleportSpell);
    }

    private void OnInit(EntityUid uid, SpellbookComponent component, ComponentInit args)
    {
        // Resolve and cache spells. Because these spells have cooldowns & charges attached to them, we cannot just
        // resolve new prototype instances every time.
        foreach (var (id, charges) in component.InstantSpells)
        {
            var spell = new InstantAction(_protoMan.Index<InstantActionPrototype>(id));
            _actionsSystem.SetCharges(spell, charges < 0 ? null : charges);
            component.Spells.Add(spell);
        }

        foreach (var (id, charges) in component.EntitySpells)
        {
            var spell = new EntityTargetAction(_protoMan.Index<EntityTargetActionPrototype>(id));
            _actionsSystem.SetCharges(spell, charges < 0 ? null : charges);
            component.Spells.Add(spell);
        }

        foreach (var (id, charges) in component.WorldSpells)
        {
            var spell = new WorldTargetAction(_protoMan.Index<WorldTargetActionPrototype>(id));
            _actionsSystem.SetCharges(spell, charges < 0 ? null : charges);
            component.Spells.Add(spell);
        }
    }

    #region Wielding
    private void OnWielded(EntityUid uid, SpellbookComponent component, ItemWieldedEvent args)
    {
        if (args.User == null)
            return;

        _actionsSystem.AddActions(args.User.Value, component.Spells, uid);
    }

    private void OnUnwielded(EntityUid uid, SpellbookComponent component, ItemUnwieldedEvent args)
    {
        if (args.User == null)
            return;

        _actionsSystem.RemoveProvidedActions(args.User.Value, uid);
    }

    /// <summary>
    ///     Let the user know they have to wield to book to use it.
    /// </summary>
    private void OnExamined(EntityUid uid, SpellbookComponent component, ExaminedEvent args)
    {
        if (!TryComp(uid, out WieldableComponent? wieldable) || wieldable.Wielded)
            return;

        args.PushText(Loc.GetString("examine-spellbook-wieldable"));
    }
    #endregion

    private void OnProjectileSpell(ProjectileSpellEvent args)
    {
        if (args.Handled)
            return;

        var xform = Transform(args.Performer);

        var angle = (args.Target.Position - xform.WorldPosition).ToAngle();
        var projectile = Spawn(args.Projectile, xform.Coordinates);

        if (HasComp<ProjectileComponent>(projectile))
            _gunSystem.FireProjectiles(args.Performer, projectile, args.Quantity, args.Spread, angle, args.Speed);
        else if (TryComp(projectile, out HitscanComponent? hitscan))
            _gunSystem.FireHitscan(args.Performer, projectile, angle, hitscan);

        args.Handled = true;
    }

    private void OnReagentSpell(ReagentEffectSpellEvent args)
    {
        if (args.Handled)
            return;

        // This is a bit janky.. but leveraging the reagent system is the easiest way to apply things like on-fire, stun, electrocute etc.
        // Alternatively, maybe reagents effects need to be generalized so that spells don't have to pretend to be metabolizing reagents....

        ReagentEffectArgs reagentArgs = new(
            args.Target,
            null,
            null,
            null,
            args.Quantity,
            EntityManager,
            args.Method);

        foreach (var reagentEffect in args.Effects)
        {
            reagentEffect.Effect(reagentArgs);
        }

        args.Handled = true;
    }

    private void OnTeleportSpell(TeleportSpellEvent args)
    {
        if (args.Handled)
            return;

        var xform = Transform(args.Performer);

        if (_mapMan.TryFindGridAt(args.Target, out var grid))
        {
            var gridPos = grid.WorldToLocal(args.Target.Position);

            xform.Coordinates = new EntityCoordinates(grid.GridEntityId, gridPos);
        }
        else
        {
            var mapEnt = _mapMan.GetMapEntityIdOrThrow(args.Target.MapId);
            xform.WorldPosition = args.Target.Position;
            xform.AttachParent(mapEnt);
        }

        args.Handled = true;
    }
}

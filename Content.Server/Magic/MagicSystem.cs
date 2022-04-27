using Content.Server.Decals;
using Content.Server.Magic.Events;
using Content.Server.Wieldable;
using Content.Server.Wieldable.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;


namespace Content.Server.Magic;

public sealed class MagicSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly DecalSystem _decals = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpellbookComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpellbookComponent, ItemWieldedEvent>(OnWielded);
        SubscribeLocalEvent<SpellbookComponent, ItemUnwieldedEvent>(OnUnWield);

        SubscribeLocalEvent<RuneMagicEvent>(OnRuneMagic);
        SubscribeLocalEvent<TeleportSpellEvent>(OnTeleportSpell);
    }

    private void OnInit(EntityUid uid, SpellbookComponent component, ComponentInit args)
    {
        foreach (var (id, charges) in component.WorldSpells)
        {
            var spell = new WorldTargetAction(_prototypeManager.Index<WorldTargetActionPrototype>(id));
            _actionsSystem.SetCharges(spell, charges < 0 ? null : charges);
            component.Spells.Add((spell));
        }
    }

    #region Wielding

    private void OnWielded(EntityUid uid, SpellbookComponent component, ItemWieldedEvent args)
    {
        if (args.User == null)
            return;

        _actionsSystem.AddActions(args.User.Value, component.Spells, uid);
    }

    private void OnUnWield(EntityUid uid, SpellbookComponent component, ItemUnwieldedEvent args)
    {
        if (args.User == null)
            return;

        _actionsSystem.RemoveProvidedActions(args.User.Value, uid);
    }

    #endregion

    private void OnRuneMagic(RuneMagicEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);
        var rune = Spawn(args.Rune, transform.Coordinates);

        args.Handled = true;

    }

    private void OnTeleportSpell(TeleportSpellEvent args)
    {

    }
}

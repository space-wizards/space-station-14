using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.CombatMode;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.GreyStation.Hailer;

/// <summary>
/// Handles action adding and usage for <see cref="HailerComponent"/>.
/// </summary>
public abstract class SharedHailerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HailerComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<HailerComponent, HailerActionEvent>(OnActionUsed);
        SubscribeLocalEvent<HailerComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnGetActions(Entity<HailerComponent> ent, ref GetItemActionsEvent args)
    {
        if ((args.SlotFlags & ent.Comp.RequiredFlags) == ent.Comp.RequiredFlags)
            args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnActionUsed(Entity<HailerComponent> ent, ref HailerActionEvent args)
    {
        // require the mask be pulled down before using
        if (TryComp<MaskComponent>(ent, out var mask) && mask.IsToggled)
            return;

        args.Handled = true;

        List<HailerLine> lines;
        if (HasComp<EmaggedComponent>(ent))
            lines = args.Emagged;
        else if (_combatMode.IsInCombatMode(args.Performer))
            lines = args.Combat;
        else
            lines = args.Normal;

        Say(ent, lines);
    }

    private void OnEmagged(Entity<HailerComponent> ent, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }

    /// <summary>
    /// Say the actual random message ingame, only done serverside.
    /// </summary>
    protected virtual void Say(EntityUid uid, List<HailerLine> lines)
    {
    }
}

using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.CombatMode;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.GreyStation.Hailer;

/// <summary>
/// Handles action adding and usage for <see cref="HailerComponent"/>.
/// </summary>
public abstract class SharedHailerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HailerComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<HailerComponent, HailerActionEvent>(OnActionUsed);
        SubscribeLocalEvent<HailerComponent, GotEmaggedEvent>(OnEmagged);

        Subs.BuiEvents<HailerComponent>(HailerUiKey.Key, subs =>
        {
            subs.Event<HailerPlayLineMessage>(OnPlayLine);
        }
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

        args.Handled = _ui.TryOpenUi(ent.Owner, HailerUiKey.Key, args.Performer, predicted: true);
    }

    private void OnLineChosen(Entity<HailerComponent> ent, ref HailerPlayLineMessage args)
    {
        var lines = GetLines(ent, args.Performer);
        if (args.Index > lines.Count)
            return;

        var line = lines[args.Index];
        _audio.PlayPredicted(line.Sound, ent);
        _actions.SetCooldown(ent.Comp.ActionEntity, ent.Comp.Cooldown);
        ent.Comp.LastMessage = line.Message;
        Say(ent, line.Message);
    }

    protected List<HailerLine> GetLines(Entity<HailerComponent> ent, EntityUid user)
    {
        if (HasComp<EmaggedComponent>(ent))
            return ent.Comp.Emagged;

        if (_combatMode.IsInCombatMode(user))
            return ent.Comp.Combat;

        return ent.Comp.Normal;
    }

    protected int PickRandomLine(Entity<HailerComponent> ent, EntityUid user)
    {
        var lines = GetLines(ent, user);
        if (lines.Count < 1)
            return 0;

        var index = 0;
        do {
            index = _random.Next(lines.Count);
        } while (lines[index].Message == ent.Comp.LastPlayed);

        return index;
    }

    private void OnEmagged(Entity<HailerComponent> ent, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }

    /// <summary>
    /// Say the actual message ingame, only done serverside.
    /// </summary>
    protected virtual void Say(Entity<HailerComponent> ent, string message)
    {
    }
}

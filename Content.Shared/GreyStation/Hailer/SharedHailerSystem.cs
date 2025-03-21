using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.CombatMode;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.GreyStation.Hailer;

/// <summary>
/// Handles action adding and usage for <see cref="HailerComponent"/>.
/// </summary>
public abstract class SharedHailerSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HailerComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<HailerComponent, HailerActionEvent>(OnActionUsed);
        SubscribeLocalEvent<HailerComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<HailerComponent, ItemMaskToggledEvent>(OnMaskToggled);

        Subs.BuiEvents<HailerComponent>(HailerUiKey.Key, subs =>
        {
            subs.Event<HailerPlayLineMessage>(OnPlayLine);
        });
    }

    private void OnGetActions(Entity<HailerComponent> ent, ref GetItemActionsEvent args)
    {
        if ((args.SlotFlags & ent.Comp.RequiredFlags) != ent.Comp.RequiredFlags)
            return;

        args.AddAction(ref ent.Comp.PickerActionEntity, ent.Comp.PickerAction);
        args.AddAction(ref ent.Comp.RandomActionEntity, ent.Comp.RandomAction);
    }

    private void OnActionUsed(Entity<HailerComponent> ent, ref HailerActionEvent args)
    {
        // require the mask be up before using
        if (TryComp<MaskComponent>(ent, out var mask) && mask.IsToggled)
            return;

        var lines = GetLines(ent, args.Performer);
        if (!args.Random && lines.Count > 1)
        {
            if (_net.IsClient)
                _ui.TryOpenUi(ent.Owner, HailerUiKey.Key, args.Performer, predicted: true);
            return;
        }

        var index = PickRandomLine(ent, args.Performer);
        // not predicted as the server is picking the line
        args.Handled = PlayLine(ent, lines[index], args.Performer, predicted: false);
    }

    private void OnPlayLine(Entity<HailerComponent> ent, ref HailerPlayLineMessage args)
    {
        var lines = GetLines(ent, args.Actor);
        if (args.Index >= lines.Count)
            return;

        if (PlayLine(ent, lines[(int) args.Index], args.Actor, predicted: true))
            _ui.CloseUi(ent.Owner, HailerUiKey.Key);
    }

    public bool PlayLine(Entity<HailerComponent> ent, HailerLine line, EntityUid user, bool predicted)
    {
        if (TryComp<UseDelayComponent>(ent, out var delay) && !_useDelay.TryResetDelay((ent, delay), true))
            return false;

        if (TryComp<MaskComponent>(ent, out var mask) && mask.IsToggled)
            return false;

        if (predicted)
            _audio.PlayPredicted(line.Sound, ent, user);
        else if (_net.IsServer)
            _audio.PlayPvs(line.Sound, ent);

        ent.Comp.LastPlayed = line.Message;
        Say(ent, line.Message, user);
        return true;
    }

    public List<HailerLine> GetLines(Entity<HailerComponent> ent, EntityUid user)
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
        if (lines.Count < 2)
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

    private void OnMaskToggled(Entity<HailerComponent> ent, ref ItemMaskToggledEvent args)
    {
        // if you pull the mask down the menu is closed
        if (TryComp<MaskComponent>(ent, out var mask) && mask.IsToggled)
            _ui.CloseUi(ent.Owner, HailerUiKey.Key);
    }

    /// <summary>
    /// Say the actual message ingame, only done serverside.
    /// </summary>
    protected virtual void Say(Entity<HailerComponent> ent, string message, EntityUid user)
    {
    }
}

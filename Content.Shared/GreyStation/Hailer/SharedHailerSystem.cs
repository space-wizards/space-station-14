using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.CombatMode;
using Content.Shared.Dataset;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.GreyStation.Hailer;

/// <summary>
/// Handles action adding and usage for <see cref="HailerComponent"/>.
/// </summary>
public abstract class SharedHailerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
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

        var emagged = HasComp<EmaggedComponent>(ent);
        var sound = ent.Comp.Sound;
        if (emagged && ent.Comp.EmaggedSound is {} emagSound)
            sound = emagSound;

        _audio.PlayPredicted(sound, ent, args.Performer);

        string datasetId;
        if (emagged)
            datasetId = args.Emagged;
        else if (_combatMode.IsInCombatMode(args.Performer))
            datasetId = args.Combat;
        else
            datasetId = args.Normal;

        var dataset = _proto.Index<DatasetPrototype>(datasetId);
        Say(ent, dataset);
    }

    private void OnEmagged(Entity<HailerComponent> ent, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }

    /// <summary>
    /// Say the actual random message ingame, only done serverside.
    /// </summary>
    protected virtual void Say(EntityUid uid, DatasetPrototype dataset)
    {
    }
}

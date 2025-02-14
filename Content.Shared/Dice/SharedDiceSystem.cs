using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Dice;

public abstract class SharedDiceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiceComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DiceComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<DiceComponent, ExaminedEvent>(OnExamined);
    }

    private void OnUseInHand(Entity<DiceComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Roll(entity, args.User);
        args.Handled = true;
    }

    private void OnLand(Entity<DiceComponent> entity, ref LandEvent args)
    {
        Roll(entity);
    }

    private void OnExamined(Entity<DiceComponent> entity, ref ExaminedEvent args)
    {
        //No details check, since the sprite updates to show the side.
        using (args.PushGroup(nameof(DiceComponent)))
        {
            args.PushMarkup(Loc.GetString("dice-component-on-examine-message-part-1", ("sidesAmount", entity.Comp.Sides)));
            args.PushMarkup(Loc.GetString("dice-component-on-examine-message-part-2",
                ("currentSide", entity.Comp.CurrentValue)));
        }
    }

    private void SetCurrentSide(Entity<DiceComponent> entity, int side)
    {
        if (side < 1 || side > entity.Comp.Sides)
        {
            Log.Error($"Attempted to set die {ToPrettyString(entity)} to an invalid side ({side}).");
            return;
        }

        entity.Comp.CurrentValue = (side - entity.Comp.Offset) * entity.Comp.Multiplier;
        Dirty(entity);
    }

    public void SetCurrentValue(Entity<DiceComponent> entity, int value)
    {
        if (value % entity.Comp.Multiplier != 0 || value / entity.Comp.Multiplier + entity.Comp.Offset < 1)
        {
            Log.Error($"Attempted to set die {ToPrettyString(entity)} to an invalid value ({value}).");
            return;
        }

        SetCurrentSide(entity, value / entity.Comp.Multiplier + entity.Comp.Offset);
    }

    private void Roll(Entity<DiceComponent> entity, EntityUid? user = null)
    {
        var rand = new System.Random((int)_timing.CurTick.Value);

        var roll = rand.Next(1, entity.Comp.Sides + 1);
        SetCurrentSide(entity, roll);

        var popupString = Loc.GetString("dice-component-on-roll-land",
            ("die", entity),
            ("currentSide", entity.Comp.CurrentValue));
        _popup.PopupPredicted(popupString, entity, user);
        _audio.PlayPredicted(entity.Comp.Sound, entity, user);
    }
}

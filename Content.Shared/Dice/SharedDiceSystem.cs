using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Throwing;
using Robust.Shared.GameStates;

namespace Content.Shared.Dice;

public abstract class SharedDiceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiceComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DiceComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<DiceComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DiceComponent, AfterAutoHandleStateEvent>(OnDiceAfterHandleState);
    }

    private void OnDiceAfterHandleState(EntityUid uid, DiceComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(uid, component);
    }

    private void OnUseInHand(EntityUid uid, DiceComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Roll(uid, component);
    }

    private void OnLand(EntityUid uid, DiceComponent component, ref LandEvent args)
    {
        Roll(uid, component);
    }

    private void OnExamined(EntityUid uid, DiceComponent dice, ExaminedEvent args)
    {
        //No details check, since the sprite updates to show the side.
        using (args.PushGroup(nameof(DiceComponent)))
        {
            args.PushMarkup(Loc.GetString("dice-component-on-examine-message-part-1", ("sidesAmount", dice.Sides)));
            args.PushMarkup(Loc.GetString("dice-component-on-examine-message-part-2",
                ("currentSide", dice.CurrentValue)));
        }
    }

    public void SetCurrentSide(EntityUid uid, int side, DiceComponent? die = null)
    {
        if (!Resolve(uid, ref die))
            return;

        if (side < 1 || side > die.Sides)
        {
            Log.Error($"Attempted to set die {ToPrettyString(uid)} to an invalid side ({side}).");
            return;
        }

        die.CurrentValue = (side - die.Offset) * die.Multiplier;
        Dirty(uid, die);
        UpdateVisuals(uid, die);
    }

    public void SetCurrentValue(EntityUid uid, int value, DiceComponent? die = null)
    {
        if (!Resolve(uid, ref die))
            return;

        if (value % die.Multiplier != 0 || value/ die.Multiplier + die.Offset < 1)
        {
            Log.Error($"Attempted to set die {ToPrettyString(uid)} to an invalid value ({value}).");
            return;
        }

        SetCurrentSide(uid, value / die.Multiplier + die.Offset, die);
    }

    protected virtual void UpdateVisuals(EntityUid uid, DiceComponent? die = null)
    {
        // See client system.
    }

    public virtual void Roll(EntityUid uid, DiceComponent? die = null)
    {
        // See the server system, client cannot predict rolling.
    }
}

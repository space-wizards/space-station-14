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

        SubscribeLocalEvent<DiceComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DiceComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DiceComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<DiceComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DiceComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<DiceComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, DiceComponent component, ref ComponentHandleState args)
    {
        if (args.Current is DiceComponent.DiceState state)
            component.CurrentSide = state.CurrentSide;

        UpdateVisuals(uid, component);
    }

    private void OnGetState(EntityUid uid, DiceComponent component, ref ComponentGetState args)
    {
        args.State = new DiceComponent.DiceState(component.CurrentSide);
    }

    private void OnComponentInit(EntityUid uid, DiceComponent component, ComponentInit args)
    {
        if (component.CurrentSide > component.Sides)
            component.CurrentSide = component.Sides;

        UpdateVisuals(uid, component);
    }

    private void OnUseInHand(EntityUid uid, DiceComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;

        args.Handled = true;
        Roll(uid, component);
    }

    private void OnLand(EntityUid uid, DiceComponent component, LandEvent args)
    {
        Roll(uid, component);
    }

    private void OnExamined(EntityUid uid, DiceComponent dice, ExaminedEvent args)
    {
        //No details check, since the sprite updates to show the side.
        args.PushMarkup(Loc.GetString("dice-component-on-examine-message-part-1", ("sidesAmount", dice.Sides)));
        args.PushMarkup(Loc.GetString("dice-component-on-examine-message-part-2", ("currentSide", dice.CurrentSide)));
    }

    public void SetCurrentSide(EntityUid uid, int side, DiceComponent? die = null)
    {
        if (!Resolve(uid, ref die))
            return;

        die.CurrentSide = side;
        Dirty(die);
        UpdateVisuals(uid, die);
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

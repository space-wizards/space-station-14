using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Throwing;
using Robust.Shared.GameStates;

namespace Content.Shared.CoinFlippable;

public abstract class SharedCoinFlippableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoinFlippableComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CoinFlippableComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<CoinFlippableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CoinFlippableComponent, AfterAutoHandleStateEvent>(OnCoinFlippableAfterHandleState);
    }

    private void OnCoinFlippableAfterHandleState(EntityUid uid, CoinFlippableComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(uid, component);
    }

    private void OnUseInHand(EntityUid uid, CoinFlippableComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Roll(uid, component);
    }

    private void OnLand(EntityUid uid, CoinFlippableComponent component, ref LandEvent args)
    {
        Roll(uid, component);
    }

    private void OnExamined(EntityUid uid, CoinFlippableComponent coinFlippable, ExaminedEvent args)
    {
        string currentVal = "error";
        switch (coinFlippable.CurrentValue) // Convert coinFlippable.CurrentValue to its proper string representation
        {
            case 0:
                currentVal ="heads";
                break;
            case 1:
                currentVal ="tails";
                break;
            case 2:
                currentVal ="its side";
                break;
        }

        //No details check, since the sprite updates to show the side.
        using (args.PushGroup(nameof(CoinFlippableComponent)))
        {
            args.PushMarkup(Loc.GetString("coin-flippable-component-on-examine-message-part-1"));
            args.PushMarkup(Loc.GetString("coin-flippable-component-on-examine-message-part-2",
                ("currentSide", currentVal)));
        }
    }

    public void SetCurrentSide(EntityUid uid, int side, CoinFlippableComponent? coinFlippable = null)
    {
        if (!Resolve(uid, ref coinFlippable))
            return;

        if (coinFlippable.CanLandOnItsSide)
        {
            if (side < 0 || side > 2)
            {
                Log.Error($"Attempted to set coinFlippable {ToPrettyString(uid)} to an invalid side ({side}).");
                return;
            }
        }
        else
        {
            if (side < 0 || side > 1)
            {
                Log.Error($"Attempted to set coinFlippable {ToPrettyString(uid)} to an invalid side ({side}).");
                return;
            }
        }

        coinFlippable.CurrentValue = side;
        Dirty(uid, coinFlippable);
        UpdateVisuals(uid, coinFlippable);
    }

    public void SetCurrentValue(EntityUid uid, int value, CoinFlippableComponent? coinFlippable = null)
    {
        if (!Resolve(uid, ref coinFlippable))
            return;

        if (coinFlippable.CanLandOnItsSide)
        {
            if (value < 0 || value > 2)
            {
                Log.Error($"Attempted to set coinFlippable {ToPrettyString(uid)} to an invalid value ({value}).");
                return;
            }
        }
        else
        {
            if (value < 0 || value > 1)
            {
                Log.Error($"Attempted to set coinFlippable {ToPrettyString(uid)} to an invalid value ({value}).");
                return;
            }
        }

        SetCurrentSide(uid, value, coinFlippable);
    }

    protected virtual void UpdateVisuals(EntityUid uid, CoinFlippableComponent? coinFlippable = null)
    {
        // See client system.
    }

    public virtual void Roll(EntityUid uid, CoinFlippableComponent? coinFlippable = null)
    {
        // See the server system, client cannot predict rolling.
    }
}

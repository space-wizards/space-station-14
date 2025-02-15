using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.CoinFlippable;

public abstract class SharedCoinFlippableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoinFlippableComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CoinFlippableComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<CoinFlippableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnUseInHand(Entity<CoinFlippableComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Roll(entity, args.User);
        args.Handled = true;
    }

    private void OnLand(Entity<CoinFlippableComponent> entity, ref LandEvent args)
    {
        Roll(entity);
    }

    private void OnExamined(Entity<CoinFlippableComponent> entity, ref ExaminedEvent args)
    {
        string currentVal = "Error";
        switch (entity.Comp.CurrentValue) // Convert CurrentValue to its proper string representation
        {
            case 0:
                currentVal = Loc.GetString("coin-flippable-result-heads");
                break;
            case 1:
                currentVal = Loc.GetString("coin-flippable-result-tails");
                break;
            case 2:
                currentVal = Loc.GetString("coin-flippable-result-side");
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

    private void SetCurrentSide(Entity<CoinFlippableComponent> entity, int side)
    {
        if (entity.Comp.CanLandOnItsSide)
        {
            if (side < 0 || side > 2)
            {
                Log.Error($"Attempted to set coin {ToPrettyString(entity)} to an invalid side ({side}).");
                return;
            }
        }
        else
        {
            if (side < 0 || side > 1)
            {
                Log.Error($"Attempted to set coin {ToPrettyString(entity)} to an invalid side ({side}).");
                return;
            }
        }

        entity.Comp.CurrentValue = side;
        Dirty(entity);
    }

    public void SetCurrentValue(Entity<CoinFlippableComponent> entity, int value)
    {
        if (entity.Comp.CanLandOnItsSide)
        {
            if (value < 0 || value > 2)
            {
                Log.Error($"Attempted to set coin {ToPrettyString(entity)} to an invalid value ({value}).");
                return;
            }
        }
        else
        {
            if (value < 0 || value > 1)
            {
                Log.Error($"Attempted to set coin {ToPrettyString(entity)} to an invalid value ({value}).");
                return;
            }
        }

        SetCurrentSide(entity, value);
    }

    private void Roll(Entity<CoinFlippableComponent> entity, EntityUid? user = null)
    {
        // Generate Random Outcome
        System.Random rand = new System.Random((int)_timing.CurTick.Value);
        float roll = rand.NextSingle()*100.0F; // Random float between 0.0f and

        int rollResult = -1;
        if (entity.Comp.CanLandOnItsSide)
        {
            if (roll < entity.Comp.PercentageSideLand)
                rollResult = 2;
            else if (roll < (entity.Comp.PercentageSideLand + (100 - entity.Comp.PercentageSideLand) / 2))
                rollResult = 0; // 50% of remaining probability
            else
                rollResult = 1; // 50% of remaining probability
        }
        else
        {
            if (roll < 50.0F)
                rollResult = 0; // 50% of remaining probability
            else
                rollResult = 1; // 50% of remaining probability
        }
        SetCurrentSide(entity, rollResult);

        // Update Description
        string currentVal = "error";
        switch (entity.Comp.CurrentValue) // Convert coinFlippable.CurrentValue to its proper string representation
        {
            case 0:
                currentVal = Loc.GetString("coin-flippable-result-heads");
                break;
            case 1:
                currentVal = Loc.GetString("coin-flippable-result-tails");
                break;
            case 2:
                currentVal = Loc.GetString("coin-flippable-result-side");
                break;
        }

        _popup.PopupPredicted(Loc.GetString("coin-flippable-component-on-roll-land",("coin",entity),("currentSide",currentVal)), entity, user);
        _audio.PlayPredicted(entity.Comp.Sound, entity, user);
    }
}

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
        // Error Handling
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

    private void Roll(Entity<CoinFlippableComponent> entity, EntityUid? user = null)
    {
        // Error Handling (Done before the roll just in case settings change)
        if (entity.Comp.UsesWeightedChances)
        {
            // Ensure Proper Dictionary Values Exist
            if (entity.Comp.CanLandOnItsSide)
            {
                if (!entity.Comp.WeightedChances.ContainsKey("heads") ||
                    !entity.Comp.WeightedChances.ContainsKey("tails") ||
                    !entity.Comp.WeightedChances.ContainsKey("side"))
                {
                    Log.Error($"The proper keys of WeightedChances on {ToPrettyString(entity)} do not exist.");
                    return;
                }
            }
            else
            {
                if (!entity.Comp.WeightedChances.ContainsKey("heads") ||
                    !entity.Comp.WeightedChances.ContainsKey("tails"))
                {
                    Log.Error($"The proper keys of WeightedChances on {ToPrettyString(entity)} do not exist.");
                    return;
                }
            }
            // Ensure Dictionary Values Sum to 100% (if using)
            if (entity.Comp.UsesWeightedChances)
            {
                if (entity.Comp.CanLandOnItsSide)
                {
                    if (entity.Comp.WeightedChances["heads"] +
                        entity.Comp.WeightedChances["tails"] +
                        entity.Comp.WeightedChances["side"] != 100.0)
                    {
                        Log.Error($"WeightedChances on {ToPrettyString(entity)} do not sum to 100%. " +
                                  $"Proof: {entity.Comp.WeightedChances["heads"]} + " +
                                  $"{entity.Comp.WeightedChances["tails"]} + " +
                                  $"{entity.Comp.WeightedChances["side"]} = " +
                                  $"{entity.Comp.WeightedChances["heads"] + entity.Comp.WeightedChances["tails"] + entity.Comp.WeightedChances["side"]}%");
                        return;
                    }
                }
                else
                {
                    if (entity.Comp.WeightedChances["heads"] +
                        entity.Comp.WeightedChances["tails"] != 100.0)
                    {
                        Log.Error($"WeightedChances on {ToPrettyString(entity)} do not sum to 100%. " +
                                  $"Proof: {entity.Comp.WeightedChances["heads"]} + " +
                                  $"{entity.Comp.WeightedChances["tails"]} = " +
                                  $"{entity.Comp.WeightedChances["heads"] + entity.Comp.WeightedChances["tails"]}%");
                        return;
                    }
                }
            }
        }

        // Generate Random Outcome
        System.Random rand = new System.Random((int)_timing.CurTick.Value);
        float roll = rand.NextSingle()*100.0F; // Random float between 0.0f and

        int rollResult = -1;
        Dictionary<string, float> chances;
        if (entity.Comp.CanLandOnItsSide) // If it can land on its side...
        {
            if (entity.Comp.UsesWeightedChances)
            {
                chances = entity.Comp.WeightedChances;
            }
            else // Defaults to Hardcoded values if customs not desired
            {
                chances = entity.Comp.DefaultChancesWithoutSide;
            }
            // Actual Roll
            if (roll < chances["heads"])
                rollResult = 0;
            else if (roll < chances["heads"]+chances["tails"])
                rollResult = 1;
            else
                rollResult = 2;
        }
        else // If it cant land on its side...
        {
            if (entity.Comp.UsesWeightedChances)
            {
                chances = entity.Comp.WeightedChances;
            }
            else // Defaults to Hardcoded values if customs not desired
            {
                chances = entity.Comp.DefaultChancesWithoutSide;
            }
            // Actual Roll
            if (roll < chances["heads"])
                rollResult = 0;
            else
                rollResult = 1;
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

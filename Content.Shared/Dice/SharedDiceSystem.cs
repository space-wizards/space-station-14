using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Dice;

public abstract partial class SharedDiceSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<DiceComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Roll(entity, args.User);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnLand(Entity<DiceComponent> entity, ref LandEvent args)
    {
        Roll(entity);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<DiceComponent> entity, ref ExaminedEvent args)
    {
        //No details check, since the sprite updates to show the side.
        using (args.PushGroup(nameof(DiceComponent)))
        {
            if (entity.Comp.ExamineObjectText != null)
            {
                args.PushMarkup(Loc.GetString("dice-component-on-examine-message-part-1", ("sidesAmount", entity.Comp.Sides), ("name", Loc.GetString(entity.Comp.ExamineObjectText))));
            }

            var valueString = GetRolledValueString(entity);
            args.PushMarkup(Loc.GetString(entity.Comp.ExamineLandedOnText, ("currentSide", valueString)));
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
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));

        // Is the dice weighted?
        if (entity.Comp.WeightedValue is { } weightedValue && rand.Prob(entity.Comp.WeightedProb))
        {
            SetCurrentSide(entity, weightedValue);
        }
        else
        {
            var roll = rand.Next(1, entity.Comp.Sides + 1);
            SetCurrentSide(entity, roll);
        }

        var popupString = Loc.GetString("dice-component-on-roll-land",
                ("die", entity),
                ("currentSide", GetRolledValueString(entity)));

        if (user == null)
            _popup.PopupEntity(popupString, entity);
        else
            _popup.PopupEntity(popupString, entity, user);
        _audio.PlayPredicted(entity.Comp.Sound, entity, user);
    }

    // Returns a readable string of the value of the dice.
    private string GetRolledValueString(Entity<DiceComponent> entity)
    {
        if (ProtoMan.TryIndex(entity.Comp.Values, out var valuesPrototype)
            && valuesPrototype.Values.Count >= entity.Comp.CurrentValue
            && entity.Comp.CurrentValue >= 1)
        {
            return Loc.GetString(valuesPrototype.Values[entity.Comp.CurrentValue - 1]);
        }
        else
        {
            return entity.Comp.CurrentValue.ToString();
        }
    }
}

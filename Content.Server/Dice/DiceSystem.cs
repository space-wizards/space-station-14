using Content.Server.Explosion.EntitySystems;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.Components.OnTrigger;
using Content.Shared.Dice;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Dice;

[UsedImplicitly]
public sealed class DiceSystem : SharedDiceSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Roll(EntityUid uid, DiceComponent? die = null)
    {
        if (!Resolve(uid, ref die))
            return;

        var roll = _random.Next(1, die.Sides + 1);
        SetCurrentSide(uid, roll, die);

        _audio.PlayPvs(die.Sound, uid);

        if (die.DiceBomb && !TryComp<ExplodeOnTriggerComponent>(uid, out var explodeTrigger)) // True only on first roll, so cannot be activated twice
        {
            DiceBombExplode(uid, die);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("dice-component-on-roll-land", ("die", uid), ("currentSide", die.CurrentValue)), uid); // So to make other popup if critical failure
        }
    }
    public void DiceBombExplode(EntityUid uid, DiceComponent dice)
    {
        if (!TryComp<TwoStageTriggerComponent>(uid, out var twoStage) || !TryComp<ExplosiveComponent>(uid, out var explosive))
            return;
        if (dice.CurrentValue == 1 || dice.CurrentValue == 2)
        {
            _popup.PopupEntity(Loc.GetString("dice-component-on-roll-land-crititcal-failure", ("die", uid), ("currentSide", dice.CurrentValue)), uid, PopupType.LargeCaution);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("dice-component-on-roll-land", ("die", uid), ("currentSide", dice.CurrentValue)), uid);
        }
        explosive.MaxIntensity = (dice.CurrentValue/3*2); // Makes 1 value on dice making 10 damag at boom center
        explosive.IntensitySlope = (dice.CurrentValue/3);
        explosive.TotalIntensity = (dice.CurrentValue/3*8);
        twoStage.TriggerDelay = TimeSpan.FromSeconds(dice.CurrentValue);
        _trigger.Trigger(uid);
    }
}

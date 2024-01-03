using Content.Server.Explosion.EntitySystems;
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

        _popup.PopupEntity(Loc.GetString("dice-component-on-roll-land", ("die", uid), ("currentSide", die.CurrentValue)), uid);
        _audio.PlayPvs(die.Sound, uid);

        // For dice bomb to boom after value seconds after landing
        if (!TryComp<TwoStageTriggerComponent>(uid, out var twostage) || !die.DiceBomb)
            return;
        DiceBombExplode(uid, die, twostage);

    }

    public void DiceBombExplode(EntityUid uid, DiceComponent component, TwoStageTriggerComponent twostage)
    {
        twostage.TriggerDelay = TimeSpan.FromSeconds(component.CurrentValue);
        _trigger.Trigger(uid);
    }
}

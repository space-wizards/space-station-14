using Content.Shared.CoinFlippable;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.CoinFlippable;

[UsedImplicitly]
public sealed class CoinFlippableSystem : SharedCoinFlippableSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Roll(EntityUid uid, CoinFlippableComponent? coinFlippable = null)
    {
        if (!Resolve(uid, ref coinFlippable))
            return;

        float roll = -1.0F;
        roll = _random.NextFloat(0.0F, 100.0F); // Float between 0 and 100
        var roll_result = -1;
        if (coinFlippable.CanLandOnItsSide)
        {
            if (roll < coinFlippable.PercentageSideLand)
                roll_result = 2;
            else if (roll < (coinFlippable.PercentageSideLand + (100 - coinFlippable.PercentageSideLand) / 2))
                roll_result = 0; // 50% of remaining probability
            else
                roll_result = 1; // 50% of remaining probability
        }
        else
        {
            if (roll < 50.0F)
                roll_result = 0; // 50% of remaining probability
            else
                roll_result = 1; // 50% of remaining probability
        }
        SetCurrentSide(uid, roll_result, coinFlippable);

        string currentVal = "error";
        switch (coinFlippable.CurrentValue) // Convert coinFlippable.CurrentValue to its proper string representation
        {
            case 0:
                currentVal ="Heads";
                break;
            case 1:
                currentVal ="Tails";
                break;
            case 2:
                currentVal ="Its Side";
                break;
        }

        _popup.PopupEntity(Loc.GetString("coin-flippable-component-on-roll-land", ("coinFlippable", uid), ("currentSide", currentVal)), uid);
        _audio.PlayPvs(coinFlippable.Sound, uid);
    }
}

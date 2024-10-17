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

    public override void Roll(EntityUid uid, DiceComponent? die = null, EntityUid? roller = null)
    {
        if (!Resolve(uid, ref die))
            return;

        var rollEvent = new DiceRollEvent(_random.Next(1, die.Sides + 1), roller);
        RaiseLocalEvent(uid, ref rollEvent);

        SetCurrentSide(uid, rollEvent.Roll, die);

        _popup.PopupEntity(Loc.GetString("dice-component-on-roll-land", ("die", uid), ("currentSide", die.CurrentValue)), uid);
        _audio.PlayPvs(die.Sound, uid);
    }
}

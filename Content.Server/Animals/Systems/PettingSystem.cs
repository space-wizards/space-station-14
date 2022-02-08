using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Server.Animals.Components;
using Content.Shared.MobState;
using Robust.Shared.Random;

namespace Content.Server.Animals.Systems;

public class PettingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PettableComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<PettableComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(EntityUid uid, PettableComponent component, MobStateChangedEvent args)
    {
        if (!args.Component.IsAlive()) // if not alive (dead, incapacitated, etc.)
            component.PetSuccessChance = -1.0f; // set to the "invalid" value, which suppresses all popups and sound effects.

        return;
    }

    private void OnInteractHand(EntityUid uid, PettableComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (component.PetDelay.TotalSeconds <= 0)
            return;

        if (_gameTiming.CurTime < component.LastPetTime + component.PetDelay)
            return;

        if (component.PetSuccessChance == -1.0f) // for the special "invalid" case where PetSuccessChance is -1 (e.g. target is deceased), suppress both popup and sound effect.
            return;

        var diceRoll = _random.NextFloat(1.0f); // roll to see if the petting attempt succeeds. Lower is better.

        var msg = ""; //blank by default

        if (diceRoll > component.PetSuccessChance) // Lower is better, so a > means the roll fails.
        {
            msg = Loc.GetString("petting-system-failure", ("target", uid)); // the animal does not wish to be pet right now
        }
        else // if roll succeeds
        {
            msg = Loc.GetString("petting-system-success", ("target", uid), ("desc", component.PetDescription)); // petting successful

            // play the sound effect only on petting success. TODO: could rework system to add different sounds on petting failure, such as a cat hissing.
            if (component.PetSound != null) // might be null if no sound is specified in the yaml.
                SoundSystem.Play(Filter.Pvs(args.Target), component.PetSound.GetSound(), Transform(args.Target).Coordinates);
        }

        _popupSystem.PopupEntity(msg, uid, Filter.Pvs(uid)); // if we get this far, display the popup, regardless of success or failure

        component.LastPetTime = _gameTiming.CurTime;
        args.Handled = true;
    }
}

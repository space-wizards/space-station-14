using System.Linq;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.EntitySystems;

public abstract partial class SharedMicrowaveSystem
{
    /// <summary>
    ///     Update the UI state of the microwave, including the microwave's current contents, cook time,
    ///     and whether or not it is actively cooking.
    /// </summary>
    /// <param name="microwave">The microwave to update.</param>
    public void UpdateUserInterfaceState(Entity<MicrowaveComponent?> microwave)
    {
        if (!Resolve(microwave.Owner, ref microwave.Comp))
            return;

        if (_userInterface.TryGetOpenUi(microwave.Owner, MicrowaveUiKey.Key, out var bui))
            bui.Update();
    }

    /// <summary>
    ///     Change the cook time of the microwave by selecting a new button index.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnSelectTime(Entity<MicrowaveComponent> ent, ref MicrowaveSelectCookTimeMessage args)
    {
        if (!HasContents(ent.AsNullable())
            || HasComp<ActiveMicrowaveComponent>(ent)
            || !_power.IsPowered(ent.Owner))
            return;

        // some validation to prevent trollage
        if (args.NewCookTime % 5 != 0 || args.NewCookTime > ent.Comp.MaxCookTime)
            return;

        ent.Comp.CurrentCookTimeButtonIndex = args.ButtonIndex;
        ent.Comp.CurrentCookTimerTime = args.NewCookTime;
        Audio.PlayPredicted(ent.Comp.ClickSound, ent, args.Actor, AudioParams.Default.WithVolume(-2));
        UpdateUserInterfaceState(ent.AsNullable());
    }
}

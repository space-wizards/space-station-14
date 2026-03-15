using System.Linq;
using Content.Server.Kitchen.Components;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Audio;

namespace Content.Server.Kitchen.EntitySystems;

public sealed partial class MicrowaveSystem
{
    /// <summary>
    ///     Update the UI state of the microwave, including the microwave's current contents, cook time,
    ///     and whether or not it is actively cooking.
    /// </summary>
    /// <param name="microwave">The microwave to update.</param>
    public void UpdateUserInterfaceState(Entity<MicrowaveComponent> microwave)
    {
        var uid = microwave.Owner;
        var component = microwave.Comp;
        var containedItems = GetNetEntityArray(component.Storage.ContainedEntities.ToArray());
        var isActive = HasComp<ActiveMicrowaveComponent>(uid);
        var state = new MicrowaveUpdateUserInterfaceState(
            containedItems,
            isActive,
            component.CurrentCookTimeButtonIndex,
            component.CurrentCookTimerTime,
            component.CurrentCookTimeEnd);

        _userInterface.SetUiState(uid, MicrowaveUiKey.Key, state);
    }

    /// <summary>
    ///     Change the cook time of the microwave by selecting a new button index.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnSelectTime(Entity<MicrowaveComponent> ent, ref MicrowaveSelectCookTimeMessage args)
    {
        if (!HasContents(ent) || HasComp<ActiveMicrowaveComponent>(ent) || !_power.IsPowered(ent.Owner))
            return;

        // some validation to prevent trollage
        if (args.NewCookTime % 5 != 0 || args.NewCookTime > ent.Comp.MaxCookTime)
            return;

        ent.Comp.CurrentCookTimeButtonIndex = args.ButtonIndex;
        ent.Comp.CurrentCookTimerTime = args.NewCookTime;
        ent.Comp.CurrentCookTimeEnd = TimeSpan.Zero;
        _audio.PlayPvs(ent.Comp.ClickSound, ent, AudioParams.Default.WithVolume(-2));
        UpdateUserInterfaceState(ent);
    }
}

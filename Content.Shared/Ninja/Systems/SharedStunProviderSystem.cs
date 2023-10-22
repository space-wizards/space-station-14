using Content.Shared.Ninja.Components;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// All interaction logic is implemented serverside.
/// This is in shared for API and access.
/// </summary>
public abstract class SharedStunProviderSystem : EntitySystem
{
    /// <summary>
    /// Set the battery field on the stun provider.
    /// </summary>
    public void SetBattery(EntityUid uid, EntityUid? battery, StunProviderComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.BatteryUid = battery;
    }

    /// <summary>
    /// Set the no power popup field on the stun provider.
    /// </summary>
    public void SetNoPowerPopup(EntityUid uid, string popup, StunProviderComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.NoPowerPopup = popup;
    }
}

using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Tracks devoured identities on the mind of the changeling, used for objectives.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingMindIdentityTrackerComponent : Component
{
    /// <summary>
    /// Amount of unique entities devoured by this changeling.
    /// </summary>
    [DataField]
    public int Devoured;

    /// <summary>
    /// Amount of unique identities gained by this changeling (not necessarily devoured).
    /// </summary>
    [DataField]
    public int Gained;
}

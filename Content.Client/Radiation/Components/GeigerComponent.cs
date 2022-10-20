using Content.Shared.Radiation.Components;

namespace Content.Client.Radiation.Components;

/// <inheritdoc/>
[RegisterComponent]
[ComponentReference(typeof(SharedGeigerComponent))]
public sealed class GeigerComponent : SharedGeigerComponent
{
    /// <summary>
    ///     Should it shows item control when equipped by player?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("showControl")]
    public bool ShowControl;

    /// <summary>
    ///     Marked true if control needs to update UI with latest component state.
    /// </summary>
    public bool UiUpdateNeeded;
}

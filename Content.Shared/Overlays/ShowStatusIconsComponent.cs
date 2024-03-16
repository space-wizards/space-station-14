using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
///     This component allows you to see status icons for simple things like job, hunger, thirst etc.
/// </summary>
/// <remarks>
///     Anything more complicated than a TryComp + TryIndex should be handled separately.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowStatusIconsComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ShowJob = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ShowHunger = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ShowThirst = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ShowMindShield = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ShowCriminalRecord = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ShowNukeOperative = false;
}

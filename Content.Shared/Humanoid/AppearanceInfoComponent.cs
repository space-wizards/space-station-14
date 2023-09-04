using Robust.Shared.GameStates;

namespace Content.Shared.Humanoid;

/// <summary>
///     Stores an entitys' initial HumanoidAppearanceComponent and Name
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AppearanceInfoComponent : Component
{
    /// <summary>
    /// What mob states the action will appear in
    /// </summary>
    [DataField("appearance"), ViewVariables(VVAccess.ReadWrite)]
    public HumanoidAppearanceComponent Appearance;

    /// <summary>
    /// The action to use.
    /// </summary>
    [DataField("name"), ViewVariables(VVAccess.ReadWrite)]
    public string Name;

    /// <summary>
    /// Whether the component has already gotten the appearance data or not- so it doesn't fetch it again.
    /// </summary>
    [DataField("fetched"), ViewVariables(VVAccess.ReadWrite)]
    public bool Fetched = false;
}

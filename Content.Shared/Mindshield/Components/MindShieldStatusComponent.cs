using Robust.Shared.GameStates;

namespace Content.Shared.Mindshield.Components;

/// <summary>
/// This component stores an entity's mindshield status for easier retrieval later on
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMindShieldSystem))]
public sealed partial class MindShieldStatusComponent : Component
{
    /// <summary>
    /// Whether the entity is protected from mind control & co.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsMindshielded = false;

    /// <summary>
    /// Whether the sec HUD will show a mindshield icon
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsVisible = false;
}

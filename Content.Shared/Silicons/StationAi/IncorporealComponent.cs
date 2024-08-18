using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Toggles vismask and sprite visibility of an entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IncorporealComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Visible = true;

    /// <summary>
    /// Alpha to have when disabled.
    /// </summary>
    [DataField]
    public float Alpha = 0.05f;

    [DataField, AutoNetworkedField]
    public float VisibleSpeedModifier = 0.85f;
}

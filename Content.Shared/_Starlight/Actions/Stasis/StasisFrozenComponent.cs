using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Actions.Stasis;

/// <summary>
/// Component that prevents an entity from performing most actions while in stasis.
/// This component is automatically added when an entity enters stasis and removed when they exit.
/// It blocks movement, interaction, speech, and other actions, but allows the exit stasis action.
/// </summary>
[RegisterComponent, Access(typeof(SharedStasisFrozenSystem))]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StasisFrozenComponent : Component
{
    /// <summary>
    /// Whether the player is also muted while in stasis.
    /// When true, prevents the entity from speaking or emoting.
    /// </summary>
    [DataField, AutoNetworkedField] public bool Muted = false;
}

using Robust.Shared.GameStates;

namespace Content.Shared.Emoting;

/// <summary>
/// Enables emoting of an entity.
/// <seealso cref="EmoteAttemptEvent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmotingComponent : Component
{
    /// <summary>
    /// Emotes attempts are cancelled if not true.
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(typeof(EmoteSystem), Friend = AccessPermissions.ReadWrite, Other = AccessPermissions.Read)]
    public bool Enabled = true;
}

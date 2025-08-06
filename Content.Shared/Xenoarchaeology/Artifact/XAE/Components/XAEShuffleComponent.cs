using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// When activated, will shuffle the position of all players
/// within a certain radius.
/// </summary>
[RegisterComponent, Access(typeof(XAEShuffleSystem)), NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XAEShuffleComponent : Component
{
    /// <summary>
    /// Radius, within which mobs would be switched.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Radius = 7.5f;
}

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Emag.Components;

/// <summary>
/// Marker component for emagged entities
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmaggedComponent : Component
{
    /// <summary>
    /// Volume of the sound that plays after entity was emagged.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SoundVolume = 20;

    /// <summary>
    /// Sound that plays after entity was emagged.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("sparks");
}

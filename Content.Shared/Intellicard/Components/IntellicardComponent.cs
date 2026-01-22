using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Intellicard;

/// <summary>
/// Allows this entity to download the station AI onto an AiHolderComponent.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IntellicardComponent : Component
{
    /// <summary>
    /// The duration it takes to download the AI from an AiHolder.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DownloadTime = 15;

    /// <summary>
    /// The duration it takes to upload the AI to an AiHolder.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int UploadTime = 3;
}

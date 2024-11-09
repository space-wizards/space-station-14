using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Server._Impstation.Container.AntiTamper;

/// <summary>
/// When a locked container with this component is destroyed, it will
/// acidify the contents.
/// </summary>
[RegisterComponent]
public sealed partial class AntiTamperComponent : Component
{
    /// <summary>
    /// List of containers to acidify. If null,
    /// all containers will acidify.
    /// </summary>
    [DataField]
    public HashSet<string>? Containers;

    [DataField]
    public LocId Message = "anti-tamper-contents-destroyed";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/soda_spray.ogg");
}

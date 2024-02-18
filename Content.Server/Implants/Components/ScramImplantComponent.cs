using Content.Server.Implants;
using Robust.Shared.Audio;

namespace Content.Server.Implants.Components;

/// <summary>
/// Randomly teleports entity when triggered.
/// </summary>
[RegisterComponent]
public sealed partial class ScramImplantComponent : Component
{
    /// <summary>
    /// Up to how far to teleport the user
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TeleportRadius = 100f;

    /// <summary>
    /// How many times to check for a valid tile to teleport to
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int TeleportAttempts = 20;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
}

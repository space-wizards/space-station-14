using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// This is used for an entity that grants a special "obey" law when emagged.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSiliconLawSystem))]
public sealed partial class EmagSiliconLawComponent : Component
{
    /// <summary>
    /// The name of the person who emagged this law provider.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? OwnerName;

    /// <summary>
    /// Does the panel need to be open to EMAG this law provider.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequireOpenPanel = true;

    /// <summary>
    /// How long the borg is stunned when it's emagged. Setting to 0 will disable it.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StunTime = TimeSpan.Zero;

    /// <summary>
    /// The sound that plays for the borg player
    /// to let them know they've been emagged
    /// </summary>
    [DataField]
    public SoundSpecifier EmaggedSound = new SoundPathSpecifier("/Audio/Ambience/Antag/emagged_borg.ogg");

}

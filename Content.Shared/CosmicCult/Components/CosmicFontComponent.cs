using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Component for Malign Fonts.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CosmicFontComponent : Component
{
    /// <summary>
    /// Wether or not the Font has been activated.
    /// </summary>
    [DataField, AutoNetworkedField] public bool Activated;

    /// <summary>
    /// Wether or not the Finale is currently active.
    /// Rather than having to query the cult system to see if the finale's active, we can just this bool here for the fonts themselves to refer to.
    /// </summary>
    [DataField, AutoNetworkedField] public bool FinaleRunning;

    /// <summary>
    /// animation states of the font moving.
    /// </summary>
    [DataField] public string InState = "base-in";

    [DataField] public string OutState = "base-out";

    [DataField] public string AnimationKey = "CosmicFont";

    [DataField] public EntProtoId Plinth = "CosmicPlinth";

    [DataField, AutoNetworkedField] public HashSet<EntProtoId> Armors =
    [
        "StellarHardsuitCosmicCult",
    ];

    [DataField, AutoNetworkedField] public HashSet<EntProtoId> Weapons =
    [
        "StellarWeaponCosmicBlade",
        "StellarWeaponCosmicLance",
        "StellarWeaponCosmicScythe",
    ];

    /// <summary>
    /// Visual and sound effects.
    /// </summary>
    [DataField] public EntProtoId GenericVfx = "CosmicGenericVfx";
    [DataField] public SoundSpecifier InsertSfx = new SoundPathSpecifier("/Audio/_ST/CosmicCult/stigma-inserted.ogg");
}

[Serializable, NetSerializable]
public enum CosmicFontVisualLayers : byte
{
    Base
}

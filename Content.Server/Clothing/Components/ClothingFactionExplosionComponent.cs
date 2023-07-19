using Content.Server.Clothing.Systems;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Server.Clothing.Components;

/// <summary>
///     If somebody from another faction equip cloyhing, this detonate
/// </summary>
//[NetworkedComponent]
[RegisterComponent]
[Access(typeof(ClothingFactionExplosionSystem))]
public sealed class ClothingFactionExplosionComponent : Component
{
    /// <summary>
    ///     friendly faction
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("friendlyFaction")]

    public String Factions = "Syndicate";
    /// <summary>
    ///     the maximum time after which the detonation will start
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("maxRandomTime")]
    public float MaxRandomTime = 60f;
    /// <summary>
    ///     chance of detonation
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),DataField("chance")]
    public float Chance = 0.9f;

}


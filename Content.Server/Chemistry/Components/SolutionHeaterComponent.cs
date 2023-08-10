using Content.Shared.Whitelist;

namespace Content.Server.Chemistry.Components;

[RegisterComponent]
public sealed class SolutionHeaterComponent : Component
{
    /// <summary>
    /// How much heat is added per second to the solution, with no upgrades.
    /// </summary>
    [DataField("baseHeatPerSecond")]
    public float BaseHeatPerSecond = 120;

    /// <summary>
    /// How much heat is added per second to the solution, taking upgrades into account.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float HeatPerSecond;

    /// <summary>
    /// The machine part that affects the heat multiplier.
    /// </summary>
    [DataField("machinePartHeatMultiplier")]
    public string MachinePartHeatMultiplier = "Capacitor";

    /// <summary>
    /// How much each upgrade multiplies the heat by.
    /// </summary>
    [DataField("partRatingHeatMultiplier")]
    public float PartRatingHeatMultiplier = 1.5f;

    /// <summary>
    /// The entities that are placed on the heater.
    /// <summary>
    [DataField("placedEntities")]
    public HashSet<EntityUid> PlacedEntities = new();

    /// <summary>
    /// The max amount of entities that can be heated at the same time.
    /// </summary>
    [DataField("maxEntities")]
    public uint MaxEntities = 1;

    /// <summary>
    /// Whitelist for entities that can be placed on the heater.
    /// </summary>
    [DataField("whitelist")]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? Whitelist;
}

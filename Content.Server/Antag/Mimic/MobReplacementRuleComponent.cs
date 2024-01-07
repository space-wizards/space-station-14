using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag.Mimic;

/// <summary>
/// Replaces the relevant entities with mobs when the game rule is started.
/// </summary>
[RegisterComponent]
public sealed partial class MobReplacementRuleComponent : Component
{
    [DataField]
    public EntProtoId Proto = "MobMimic";

    [DataField]
    public EntityWhitelist Whitelist = new()
    {
        Components = new string[]
        {
            "VendingMachine",
        }
    };

    /// <summary>
    /// Chance per-entity.
    /// </summary>
    [DataField]
    public float Chance = 0.001f;
}

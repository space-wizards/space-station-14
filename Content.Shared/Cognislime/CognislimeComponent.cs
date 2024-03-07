using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Cognislime;

/// <summary>
/// Makes objects the entity is applied to sentient and a ghost role.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCognislimeSystem))]
public sealed partial class CognislimeComponent : Component
{
    /// <summary>
    /// How long it takes to apply the item to an entity.
    /// </summary>
    [DataField("applyDuration")]
    public TimeSpan ApplyCognislimeDuration = TimeSpan.FromSeconds(3);

    [DataField("canSpeak")]
    public bool CanSpeak = true;

    [DataField("canMove")]
    public bool CanMove = false;

    [DataField("canAttack")]
    public bool CanAttack = false;

    [DataField("whitelist")]
    public EntityWhitelist? Whitelist = new()
    {
        Components =
        [
            "EntityStorage",
            "Item",
            "ReagentTank",
            "Singularity",
        ]
    };

    [DataField("blacklist")]
    public EntityWhitelist? Blacklist = new()
    {
        Components =
        [
            "PAI",
            "MindContainer",
            "BorgBrain",
        ]
    };
}

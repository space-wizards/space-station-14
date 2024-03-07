using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cognislime;

/// <summary>
/// Makes the target sentient.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CognislimeComponent : Component
{
    /// <summary>
    /// How long it takes to apply the slime to an entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("applyDuration"), AutoNetworkedField]
    public TimeSpan ApplyCognislimeDuration = TimeSpan.FromSeconds(3);

    [ViewVariables(VVAccess.ReadWrite), DataField("canSpeak"), AutoNetworkedField]
    public bool CanSpeak = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("canMove"), AutoNetworkedField]
    public bool CanMove = false;

    [ViewVariables(VVAccess.ReadWrite), DataField("whitelist"), AutoNetworkedField]
    public EntityWhitelist? Whitelist = new()
    {
        Components = new[]
        {
            "EntityStorage",
            "Item",
            "ReagentTank",
        }
    };

    [ViewVariables(VVAccess.ReadWrite), DataField("blacklist"), AutoNetworkedField]
    public EntityWhitelist? Blacklist = new()
    {
        Components = new[]
        {
            "PAI",
        }
    };
}

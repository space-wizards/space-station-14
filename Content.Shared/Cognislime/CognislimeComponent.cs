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

    /// <summary>
    /// Sound that gets played when slime is applied.
    /// </summary>
    /// <returns></returns>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundCognislime"), AutoNetworkedField]
    public SoundSpecifier? CognislimeSound = new SoundPathSpecifier("/Audio/Items/Mining/fultext_deploy.ogg");
}

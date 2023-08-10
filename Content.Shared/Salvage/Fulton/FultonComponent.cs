using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Salvage.Fulton;

/// <summary>
/// Applies <see cref="FultonedComponent"/> to the target so they teleport to <see cref="FultonBeaconComponent"/> after a time.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FultonComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("applyDuration"), AutoNetworkedField]
    public TimeSpan ApplyFultonDuration = TimeSpan.FromSeconds(3);

    [ViewVariables(VVAccess.ReadWrite), DataField("beacon")]
    public EntityUid Beacon;

    [ViewVariables(VVAccess.ReadWrite), DataField("whitelist"), AutoNetworkedField]
    public EntityWhitelist? Whitelist = new()
    {
        Components = new string[]
        {
            "EntityStorage",
            "Item",
        }
    };

    /// <summary>
    /// Sound that gets played when fulton is applied.
    /// </summary>
    /// <returns></returns>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundFulton"), AutoNetworkedField]
    public SoundSpecifier? FultonSound = new SoundPathSpecifier("/Audio/Items/Mining/fultext_deploy.ogg");
}

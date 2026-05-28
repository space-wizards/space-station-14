using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Target for approved orders to spawn at.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TradeStationComponent : Component
{
    /// <summary>
    /// Has the hack been successfully completed? Modified by CargoSystem.Hack.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HackCompleted = false;

    /// <summary>
    /// How many seconds does a hacking beacon need to be planted to this to successfully hijack the ATS?
    /// </summary>
    [DataField]
    public TimeSpan HackCompletionTime = TimeSpan.FromSeconds(200);

    /// <summary>
    /// How much cash should be withdrawn from each department account upon a hijacking?
    /// </summary>
    [DataField]
    public int Fine = 5000;

    /// <summary>
    /// The sound to play in the announcement of the hijacking.
    /// </summary>
    [DataField]
    public SoundSpecifier AnnounceSound = new SoundPathSpecifier("/Audio/Misc/notice1.ogg");

    /// <summary>
    /// The sound to play in the announcement of the hijacking being a failure.
    /// </summary>
    [DataField]
    public SoundSpecifier DeactivateSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");
}

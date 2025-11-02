using Robust.Shared.GameStates;

namespace Content.Shared.Instruments;

/// <summary>
/// Shared component representing an instrument that can be played.
/// Stores only network-synchronized data used by both client and server.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InstrumentComponent : Component
{
    /// <summary>
    /// True if the instrument is currently being played by a user.
    /// </summary>
    [AutoNetworkedField]
    public bool Playing = false;

    /// <summary>
    /// The current player using this instrument, if any.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? CurrentPlayer;

    /// <summary>
    /// The sound bank or instrument preset (e.g., "piano", "guitar").
    /// </summary>
    [AutoNetworkedField]
    public string Program = "piano";

    /// <summary>
    /// Whether this instrument accepts external MIDI input (e.g. keyboard devices).
    /// </summary>
    [AutoNetworkedField]
    public bool AllowMidiInput = true;
}

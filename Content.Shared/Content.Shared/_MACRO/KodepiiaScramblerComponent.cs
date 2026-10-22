using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._MACRO.Species.Kodepiia.Components;

/// <summary>
/// This component gives the entity that has it an action that lets it scramble their own appearance.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class KodepiiaScramblerComponent : Component
{
    /// <summary>
    /// The action entity itself.
    /// </summary>
    [DataField]
    public EntityUid? ScramblerAction;

    /// <summary>
    /// The action's ID.
    /// </summary>
    [DataField]
    public string? ScramblerActionId = "ActionKodepiiaScrambler";

    /// <summary>
    /// Sound played when scrambling starts.
    /// </summary>
    [DataField]
    public SoundSpecifier ScramblerSound = new SoundPathSpecifier("/Audio/_MACRO/Voice/Kodepiia/kodescramble/kodescramble.ogg");

    /// <summary>
    /// Popup that occurs when scrambling starts.
    /// </summary>
    [DataField]
    public LocId OnScrambleStart = "kodepiia-scramble-others";

    /// <summary>
    /// Popup that occurs when scrambling ends.
    /// </summary>
    [DataField]
    public LocId OnScrambleCompleted = "kodepiia-scramble-self";
}

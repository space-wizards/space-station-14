using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Interaction.Components;

[RegisterComponent, Access(typeof(InteractionPopupSystem))]
public sealed partial class InteractionPopupComponent : Component
{
    /// <summary>
    /// Time delay between interactions to avoid spam.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan InteractDelay = TimeSpan.FromSeconds(1.0);

    /// <summary>
    /// String will be used to fetch the localized message to be played if the interaction succeeds.
    /// Nullable in case none is specified on the yaml prototype.
    /// </summary>
    [DataField]
    public LocId? InteractSuccessString;

    /// <summary>
    /// String will be used to fetch the localized message to be played if the interaction fails.
    /// Nullable in case no message is specified on the yaml prototype.
    /// </summary>
    [DataField]
    public LocId? InteractFailureString;

    /// <summary>
    /// Sound effect to be played when the interaction succeeds.
    /// Nullable in case no path is specified on the yaml prototype.
    /// </summary>
    [DataField]
    public SoundSpecifier? InteractSuccessSound;

    /// <summary>
    /// Sound effect to be played when the interaction fails.
    /// Nullable in case no path is specified on the yaml prototype.
    /// </summary>
    [DataField]
    public SoundSpecifier? InteractFailureSound;

    /// <summary>
    /// a prototype that will spawn upon successful interaction (as planned only for special effects)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? InteractSuccessSpawn;

    /// <summary>
    /// a prototype that will spawn upon failure interaction (as planned only for special effects)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? InteractFailureSpawn;

    /// <summary>
    /// Chance that an interaction attempt will succeed.
    /// 1   = always play "success" popup and sound.
    /// 0.5 = 50% chance to play either success or failure popup and sound.
    /// 0   = always play "failure" popup and sound.
    /// </summary>
    [DataField]
    public float SuccessChance = 1.0f; // Always succeed, unless specified otherwise on the yaml prototype.

    /// <summary>
    /// If set, shows a message to all surrounding players but NOT the current player.
    /// </summary>
    [DataField]
    public string? MessagePerceivedByOthers;

    /// <summary>
    /// Will the sound effect be perceived by entities not involved in the interaction?
    /// </summary>
    [DataField]
    public bool SoundPerceivedByOthers = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastInteractTime;
}

using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Shared.VoiceMask;

/// <summary>
///     This component is for voice mask items & voice-masking entities! Adding this component to clothing will give the voice mask UI
///     and allow the wearer to change their voice and verb at will.
///     Having this on an entity while the IsInnate field is true will give it an innate voice masking ability.
/// </summary>
/// <remarks>
///     DO NOT use this if you do not want the interface.
///     The VoiceOverrideSystem is probably what your looking for (Or you might have to make something similar)!
/// </remarks>
[RegisterComponent]
public sealed partial class VoiceMaskComponent : Component
{
    /// <summary>
    ///     The name that will override an entities default name. If null, it will use the default override.
    /// </summary>
    [DataField]
    public string? VoiceMaskName = null;

    /// <summary>
    ///     The speech verb that will override an entities default one. If null, it will use the entities default verb.
    /// </summary>
    [DataField]
    public ProtoId<SpeechVerbPrototype>? VoiceMaskSpeechVerb;

    /// <summary>
    ///     If true will override the users identity with whatever <see cref="VoiceMaskName"/> is.
    /// </summary>
    [DataField]
    public bool OverrideIdentity;

    /// <summary>
    ///     The action that gets displayed when the voice mask is equipped.
    /// </summary>
    [DataField]
    public EntProtoId Action = "ActionChangeVoiceMask";

    /// <summary>
    ///     Reference to the action.
    /// </summary>
    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    ///     If user's voice is getting changed when they speak.
    /// </summary>
    [DataField]
    public bool Active = false;

    /// <summary>
    ///     If user's accent is getting hidden when they speak.
    /// </summary>
    [DataField]
    public bool AccentHide = true;

    /// <summary>
    ///     If user's equipped agent id name is getting changed.
    /// </summary>
    [DataField]
    public bool ChangeIDName = false;

    /// <summary>
    ///     Whether the voice mask is innate to the entity.
    ///     When added to an entity while this field is set to true, the entity itself will gain the action & UI necessary to change its voice.
    ///     When this field is set to false, then the entity with this component will be a provider (either through implanting or through wearing) of the voice masking abilities for another entity.
    /// </summary>
    [DataField]
    public bool IsInnate = false;

    /// <summary>
    ///     Is used as the title text in the UI.
    /// </summary>
    [DataField]
    public LocId TitleText = "voice-mask-name-change-window";
}


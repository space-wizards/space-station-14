using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will cause a screech to happen at a location, similiar to <see cref="ScreechActionComponent"/>
/// If TargetUser is true then their location will be used.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScreechOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The range of the screech's effects.
    /// </summary>
    [DataField]
    public float Range = 6f;

    /// <summary>
    /// Entity that will be spawned attached to the the screecher to display effects.
    /// </summary>
    [DataField]
    public EntProtoId? Vfx = "EffectScreech";

    /// <summary>
    /// Sound that will be played by the screech.
    /// </summary>
    [DataField]
    public SoundSpecifier? ScreechSound = new SoundPathSpecifier("/Audio/Effects/Screech/changeling_screech_strong.ogg", AudioParams.Default.WithVolume(1f));

    /// <summary>
    /// Entity effects applied to entities that heard the screech.
    /// </summary>
    [DataField]
    public List<EntityEffect> Effects = [];
}

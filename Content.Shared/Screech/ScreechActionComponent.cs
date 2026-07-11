using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Screech;

/// <summary>
/// Stores a screech action's parameters. Must be paired with <see cref="ScreechActionEvent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ScreechActionComponent : Component
{
    /// <summary>
    /// The range of the screech's effects.
    /// </summary>
    [DataField]
    public float Range = 6f;

    /// <summary>
    /// Entity that will be spawned in a container on the screecher to display effects.
    /// </summary>
    [DataField]
    public EntProtoId? Vfx = "EffectScreech";

    /// <summary>
    /// Sound that will be played by the screech.
    /// </summary>
    [DataField]
    public SoundSpecifier? ScreechSound;

    /// <summary>
    /// Range at which the sound will be heard.
    /// </summary>
    [DataField]
    public float SoundRange = 20f;

    /// <summary>
    /// Entity effects applied to entities that heard the screech.
    /// </summary>
    [DataField]
    public List<EntityEffect> Effects = [];
}

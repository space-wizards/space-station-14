using Content.Shared.Actions;

namespace Content.Shared.Magic.Events;

public sealed partial class LightningSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    /// <summary>
    /// Type of lightning. See lightning.yml for the different types.
    /// </summary>
    [DataField(required: true)]
    public string LightningPrototype { get; private set; }

    /// <summary>
    /// Number of bolts fired after hitting main target.
    /// </summary>
    [DataField(required: true)]
    public int BoltCount { get; private set; }

    /// <summary>
    /// Range of bolts fired after hitting main target.
    /// </summary>
    [DataField(required: true)]
    public float BoltRange { get; private set; }

    /// <summary>
    /// Number of times a bolt can spawn other bolts.
    /// </summary>
    [DataField(required: true)]
    public int ArcDepth { get; private set; }

    /// <summary>
    /// Should lightning events be triggered?
    /// </summary>
    [DataField(required: true)]
    public bool TriggerLightningEvents { get; private set; }

    /// <summary>
    /// Text said by caster.
    /// </summary>
    [DataField]
    public string? Speech { get; private set; }
}

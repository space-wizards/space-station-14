using Content.Shared.Actions;

namespace Content.Shared.Magic.Events;

public sealed partial class LightningSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField(required: true)]
    public string LightningPrototype { get; private set; }

    /// <summary>
    /// How many bolts should be fired after hitting main target.
    /// </summary>
    [DataField(required: true)]
    public int BoltCount { get; private set; }

    /// <summary>
    /// Range of bolts fired after hitting main target.
    /// </summary>
    [DataField(required: true)]
    public float BoltRange { get; private set; }

    [DataField]
    public string? Speech { get; private set; }
}

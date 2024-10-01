using Content.Shared.Actions;

namespace Content.Shared.Magic.Events;

public sealed partial class LightningSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField(required:true)]
    public string LightningPrototype { get; private set; }

    [DataField]
    public string? Speech { get; private set; }
}

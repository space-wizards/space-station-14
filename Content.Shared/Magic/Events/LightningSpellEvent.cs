using Content.Shared.Actions;

namespace Content.Shared.Magic.Events;

public sealed partial class LightningSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    // TODO: Variations of lightning

    [DataField]
    public string? Speech { get; private set; }
}

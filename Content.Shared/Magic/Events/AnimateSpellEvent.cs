using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Events;

public sealed partial class AnimateSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField]
    public string? Speech { get; private set; }

    [DataField]
    public ComponentRegistry Components = new();
}

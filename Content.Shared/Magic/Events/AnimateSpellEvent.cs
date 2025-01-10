using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Shared.Magic.Events;

public sealed partial class AnimateSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField]
    public string Task { get; private set; }

    [DataField]
    public string Faction { get; private set; }

    [DataField]
    public string AttackAnimation { get; private set; }

    [DataField]
    public SoundSpecifier AttackSound = new SoundPathSpecifier("/Audio/Weapons/smash.ogg");

    [DataField]
    public DamageSpecifier Damage = default!;

    [DataField]
    public string? Speech { get; private set; }
}

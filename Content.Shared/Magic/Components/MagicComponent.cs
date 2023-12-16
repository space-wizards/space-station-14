using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Components;
// TODO: Networked?
// TODO: Attribute for datafields
[RegisterComponent, Access(typeof(SharedMagicSystem))]
public sealed partial class MagicComponent : Component
{
    // Spawning ent?
    // Might be better off on specific events?
    // public ProtoId<EntityPrototype> Prototype = default!;

    // TODO: Use in ISpeakSpellEvent or something
    /// <summary>
    /// To say something when a spell is cast
    /// </summary>
    public string? Speech { get; private set; }

    // TODO: Is this needed if you can just set everything on the proto?
    // Vars set here will override action settings
    /// <summary>
    /// How long until the spell can be cast again?
    /// </summary>
    [DataField("cooldown")]
    public TimeSpan? Cooldown;

    // TODO: Is this needed if you can just set everything on the proto?
    /// <summary>
    /// How many times can the spell be used before <see cref="Cooldown"/>?
    /// </summary>
    [DataField("uses")]
    public int Uses = 1;

    // TODO: Doafter required (ie chanting spell)
    //  Move while casting allowed

    // TODO: Spell requirements
    //  A list of requirements to cast the spell
    //    Robes
    //    Voice
    //    Hands
    //    Wand in hand (or any item in hand
    //    Spell takes up an inhand slot
}

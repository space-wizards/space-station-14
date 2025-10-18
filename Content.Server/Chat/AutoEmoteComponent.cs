namespace Content.Server.Chat;

using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

/// <summary>
/// Causes an entity to automatically emote at a set interval.
/// </summary>
[RegisterComponent, Access(typeof(AutoEmoteSystem))]
public sealed partial class AutoEmoteComponent : Component
{
    /// <summary>
    /// A set of emotes that the entity will preform.
    /// <see cref="AutoEmotePrototype"/>
    /// </summary>
    [DataField("emotes", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AutoEmotePrototype>)), ViewVariables(VVAccess.ReadOnly)]
    public HashSet<string> Emotes = new HashSet<string>();

    /// <summary>
    /// A dictionary storing the time of the next emote attempt for each emote.
    /// Uses AutoEmotePrototype IDs as keys.
    /// <summary>
    [ViewVariables(VVAccess.ReadOnly)] //TODO: make this a datafield and (de)serialize values as time offsets when https://github.com/space-wizards/RobustToolbox/issues/3768 is fixed
    public Dictionary<string, TimeSpan> EmoteTimers = new Dictionary<string, TimeSpan>();

    /// <summary>
    /// Time of the next emote. Redundant, but avoids having to iterate EmoteTimers each update.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextEmoteTime = TimeSpan.MaxValue;
}

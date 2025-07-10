using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Mobs;

/// <summary>
///     Only applies to mobs in crit capable of ghosting/succumbing
/// </summary>
public sealed partial class CritSuccumbEvent : InstantActionEvent
{
}

/// <summary>
///     Only applies/has functionality to mobs in crit that have <see cref="DeathgaspComponent"/>
/// </summary>
public sealed partial class CritFakeDeathEvent : InstantActionEvent
{
}

/// <summary>
///     Only applies to mobs capable of speaking, as a last resort in crit
/// </summary>
public sealed partial class CritLastWordsEvent : InstantActionEvent
{
}

/// <summary>
///     Only applies to mobs capable of speaking, as a last resort in crit.
///     Raised by the client, when the last words are ready to be said.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CritLastWordsSayEvent(string Message) : EntityEventArgs
{
    public string Message = Message;
}

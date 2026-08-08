// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.PersonnelRecords;

/// <summary>
/// Current employment status of a crewmember, tracked by the Personnel Records console.
/// Mirrors the "ladder" of disciplinary actions: a reprimand can only be issued from
/// <see cref="None"/>, while a demotion or dismissal can be issued from either state.
/// </summary>
[Serializable, NetSerializable]
public enum EmploymentStatus : byte
{
    /// <summary>
    /// No active disciplinary order.
    /// </summary>
    None = 0,

    /// <summary>
    /// A reprimand has been issued. No HUD icon. Blocks issuing another reprimand.
    /// </summary>
    Reprimand,

    /// <summary>
    /// A demotion order is active and awaiting execution via the ID card console.
    /// </summary>
    Demotion,

    /// <summary>
    /// A dismissal order is active and awaiting execution via the ID card console.
    /// </summary>
    Dismissal,
}

/// <summary>
/// The kind of entry recorded in a <see cref="PersonnelHistory"/> line.
/// </summary>
[Serializable, NetSerializable]
public enum PersonnelActionType : byte
{
    Reprimand = 0,
    Demotion,
    Dismissal,

    /// <summary>
    /// An active order was cancelled before it was executed.
    /// </summary>
    Annul,

    /// <summary>
    /// An active order was executed (job title changed to match the order).
    /// </summary>
    Executed,
}

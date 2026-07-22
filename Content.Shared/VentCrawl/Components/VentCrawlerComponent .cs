using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.VentCrawl.Components;

/// <summary>
/// Enables vent-crawling for an entity, or for the wearer when placed on equipped clothing.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VentCrawlerComponent : Component
{
    /// <summary>
    /// Delay required to enter a vent.
    /// </summary>
    [DataField]
    public TimeSpan EnterDelay = TimeSpan.FromSeconds(2f);

    /// <summary>
    /// Delay required to exit a vent.
    /// </summary>
    [DataField]
    public TimeSpan ExitDelay = TimeSpan.FromSeconds(2f);
}

/// <summary>
/// Do-after event used when entering a vent tube.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class EnterVentDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Do-after event used when exiting a vent tube.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ExitVentDoAfterEvent : SimpleDoAfterEvent;

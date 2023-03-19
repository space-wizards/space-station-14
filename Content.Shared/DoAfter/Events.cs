using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter;

// This file contains a whole bunch DoAFterEvents specific to some server-side system / interaction.
// Really these should all go into their respective shared namespaces whenever those systems get properly predicted.

[Serializable, NetSerializable]
public sealed class SpikeDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class SharpDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class PoweredLightDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class SpellbookDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class GrabberDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class InsertEquipmentEvent : SimpleDoAfterEvent
{
}
/// <summary>
///     Event raised when the battery is successfully removed from the mech,
///     on both success and failure
/// </summary>
[Serializable, NetSerializable]
public sealed class RemoveBatteryEvent : SimpleDoAfterEvent
{
}

/// <summary>
///     Event raised when a person enters a mech, on both success and failure
/// </summary>
[Serializable, NetSerializable]
public sealed class MechEntryEvent : SimpleDoAfterEvent
{
}

/// <summary>
///     Event raised when a person removes someone from a mech,
///     on both success and failure
/// </summary>
[Serializable, NetSerializable]
public sealed class MechExitEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class NukeDisarmDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class HealthAnalyzerDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class HealingDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class StethoscopeDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class ReclaimerDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class StickyDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class SoulEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class HarvestEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class ResistLockerDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class EscapeInventoryEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class ApcToolFinishedEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class WieldableDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
///     Raised after welding do_after has finished. It doesn't guarantee success,
///     use <see cref="WeldableChangedEvent"/> to get updated status.
/// </summary>
[Serializable, NetSerializable]
public sealed class WeldFinishedEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class TeleporterDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class BluespaceLockerDoAfterEvent : SimpleDoAfterEvent
{
}

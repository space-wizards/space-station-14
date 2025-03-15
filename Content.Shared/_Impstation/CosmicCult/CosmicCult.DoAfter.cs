using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.CosmicCult;

[Serializable, NetSerializable]
public sealed partial class EventCosmicSiphonDoAfter : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class EventCosmicBlankDoAfter : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class EventAbsorbRiftDoAfter : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class EventPurgeRiftDoAfter : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class StartFinaleDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class CancelFinaleDoAfterEvent : SimpleDoAfterEvent { }


// Rogue Ascended
[Serializable, NetSerializable]
public sealed partial class EventRogueInfectionDoAfter : SimpleDoAfterEvent { }

using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Heretic.Prototypes;



[Serializable, NetSerializable, DataDefinition] public sealed partial class EventHereticAscension : EntityEventArgs { }
[Serializable, NetSerializable, DataDefinition] public sealed partial class EventHereticRerollTargets : EntityEventArgs { }
[Serializable, NetSerializable, DataDefinition] public sealed partial class EventHereticUpdateTargets : EntityEventArgs { }

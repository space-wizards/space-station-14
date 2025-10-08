using Content.Server.StationEvents.Events;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(MadnessShuttleEventRule))]
public sealed partial class MadnessShuttleEventRuleComponent : Component { }

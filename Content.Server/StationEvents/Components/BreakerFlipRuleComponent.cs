using Content.Server.StationEvents.Events;
using Content.Shared.Whitelist;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(BreakerFlipRule))]
public sealed partial class BreakerFlipRuleComponent : Component;

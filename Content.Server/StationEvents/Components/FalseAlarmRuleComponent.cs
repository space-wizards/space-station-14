// SPDX-License-Identifier: MIT

using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(FalseAlarmRule))]
public sealed partial class FalseAlarmRuleComponent : Component
{

}

using Content.Server.StationEvents.Events;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(WanderingPortalsRule))]
public sealed partial class WanderingPortalsRuleComponent : Component
{
    /// Portal prototype
    /// </summary>
    [DataField]
    public EntProtoId PortalPrototype = "TemporaryWanderingPortal";

    /// Minimum number of portals. If an odd number of portals is created, some will not be linked.
    /// </summary>
    [DataField]
    public int MinPortals = 4;

    /// Maximum number of portals
    /// </summary>
    [DataField]
    public int MaxPortals = 10;

    /// Should the wandering portals be able to pick up stationary objects? If disabled, a lotta lockers are gonna get displaced.
    /// </summary>
    [DataField]
    public bool IgnoreStationaryObjects = true;
}

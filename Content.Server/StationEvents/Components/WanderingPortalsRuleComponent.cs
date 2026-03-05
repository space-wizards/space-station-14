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
    /// <summary>
    /// Basic portal prototype
    /// </summary>
    [DataField]
    public EntProtoId PortalPrototype = "TemporaryWanderingPortal";

    /// <summary>
    /// Gravity well prototype
    /// </summary>
    [DataField]
    public EntProtoId GravityWellPrototype = "TemporaryWanderingGravityWell";

    /// <summary>
    /// Gravity spout prototype
    /// </summary>
    [DataField]
    public EntProtoId GravitySpoutPrototype = "TemporaryWanderingGravitySpout";

    /// <summary>
    /// Can basic portals spawn?
    /// </summary>
    [DataField]
    public bool AllowBasic = true;

    /// <summary>
    /// Can gravity portals spawn? If AllowBasic and AllowGravity are both false, it defaults to basic portals.
    /// </summary>
    [DataField]
    public bool AllowGravity = true;

    /// <summary>
    /// Can an odd number of basic portals spawn, leaving one unlinked? Gravity portals will never spawn unlinked.
    /// </summary>
    [DataField]
    public bool AllowOdd = true;

    /// <summary>
    /// Minimum number of grav portal pairs.
    /// </summary>
    [DataField]
    public int MinGravPortalPairs = 2;

    /// <summary>
    /// Minimum number of grav portal pairs when basic portals are enabled.
    /// </summary>
    [DataField]
    public int MinGravPortalPairsWhenBasic = 0;

    /// <summary>
    /// Maximum number of grav portal pairs.
    /// </summary>
    [DataField]
    public int MaxGravPortalPairs = 3;

    /// <summary>
    /// Minimum number of basic portals.
    /// </summary>
    [DataField]
    public int MinBasicPortals = 4;

    /// <summary>
    /// Maximum number of basic portals.
    /// </summary>
    [DataField]
    public int MaxBasicPortals = 10;

    /// <summary>
    /// The number removed from the basic portal count when a grav portal pair is created.
    /// </summary>
    [DataField]
    public int GravPortalPairCost = 3;

    /// <summary>
    /// Should the basic wandering portals be able to pick up stationary objects, and cause as much mayhem as the gravity portals?
    /// </summary>
    [DataField]
    public bool IgnoreStationaryObjects = true;
}

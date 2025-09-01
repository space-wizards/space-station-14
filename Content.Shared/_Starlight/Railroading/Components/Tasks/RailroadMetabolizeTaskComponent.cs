using System;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Starlight.Railroading;

/// <summary>
/// Components like Task describe the objectives that need to be completed and store progress.
/// Ideally, progress should be separated, but I’d rather not create a separate progress component for each one.
/// </summary>
[RegisterComponent]
public sealed partial class RailroadMetabolizeTaskComponent : Component
{
    [DataField]
    public string Message = "rail-metabolize-task";

    [DataField]
    public HashSet<ReagentQuantity> Reagents = [];
    
    public Dictionary<string, FixedPoint2> MetabolizedReagents = [];

    [DataField]
    public SpriteSpecifier Icon;
}


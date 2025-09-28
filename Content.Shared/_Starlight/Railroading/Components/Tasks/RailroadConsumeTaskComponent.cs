using System;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Starlight.Railroading;

/// <summary>
/// Components like Task describe the objectives that need to be completed and store progress.
/// Ideally, progress should be separated, but I’d rather not create a separate progress component for each one.
/// </summary>
[RegisterComponent]
public sealed partial class RailroadConsumeTaskComponent : Component
{
    [DataField]
    public string Message = "rail-consume-task";

    [DataField]
    public HashSet<EntProtoId> Objects = [];

    [DataField]
    public SpriteSpecifier Icon;

    [DataField]
    public bool IsCompleted;
}

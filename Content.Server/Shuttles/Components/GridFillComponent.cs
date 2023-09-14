using Content.Server.Shuttles.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// If added to an airlock will try to autofill a grid onto it on MapInit
/// </summary>
[RegisterComponent, Access(typeof(ShuttleSystem))]
public sealed partial class GridFillComponent : Component
{
    [DataField("path")] public ResPath Path = new("/Maps/Shuttles/escape_pod_small.yml");
}

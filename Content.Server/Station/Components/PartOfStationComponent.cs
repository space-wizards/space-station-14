using Content.Server.GameTicking;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Station;

/// <summary>
///     Added to grids saved in maps to designate them as 'part of a station' and not main grids. I.e. ancillary
///     shuttles for multi-grid stations.
/// </summary>
[RegisterComponent]
[Friend(typeof(GameTicker))]
public class PartOfStationComponent : Component
{
    [DataField("id", required: true)] // does yamllinter even lint maps for required fields?
    [ViewVariables(VVAccess.ReadWrite)]
    public string Id = default!;
}

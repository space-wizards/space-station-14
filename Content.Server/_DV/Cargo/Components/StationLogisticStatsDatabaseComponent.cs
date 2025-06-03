using Content.Server._DV.Cargo.Systems;
using Content.Shared._DV.CartridgeLoader.Cartridges;
using Content.Shared.Cargo;

namespace Content.Server._DV.Cargo.Components;

/// <summary>
/// Added to the abstract representation of a station to track stats related to mail delivery and income
/// </summary>
[RegisterComponent, Access(typeof(LogisticStatsSystem))]
public sealed partial class StationLogisticStatsComponent : Component
{
    [DataField]
    public MailStats Metrics;
}

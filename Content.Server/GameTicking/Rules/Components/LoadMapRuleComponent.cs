using Content.Server.GameTicking.Rules;
using Content.Server.Maps;
using Content.Shared.GridPreloader.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This is used for a game rule that loads a map when activated.
/// Works with <see cref="RuleGridsComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(LoadMapRuleSystem))]
public sealed partial class LoadMapRuleComponent : Component
{
    /// <summary>
    /// A <see cref="GameMapPrototype"/> to load on a new map.
    /// </summary>
    [DataField]
    public ProtoId<GameMapPrototype>? GameMap;

    /// <summary>
    /// A map to load.
    /// </summary>
    [DataField]
    public ResPath? MapPath;

    /// <summary>
    /// A grid to load on a new map.
    /// </summary>
    [DataField]
    public ResPath? GridPath;

    /// <summary>
    /// A <see cref="PreloadedGridPrototype"/> to move to a new map.
    /// If there are no instances left nothing is done.
    /// <para>
    /// This is deprecated. Do not create new content that uses this field,
    /// and migrate existing content to be loaded dynamically during the round.
    /// </para>
    /// </summary>
    [DataField, Obsolete("Do not pre-load grids. This causes the server to have to keep that grid loaded in memory during the entire round, even if that grid is never summoned to the playspace.")]
    public ProtoId<PreloadedGridPrototype>? PreloadedGrid;
}

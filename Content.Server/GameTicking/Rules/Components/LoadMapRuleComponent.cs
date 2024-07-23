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
    /// A map path to load on a new map.
    /// </summary>
    [DataField]
    public ResPath? MapPath;

    /// <summary>
    /// A <see cref="PreloadedGridPrototype"/> to move to a new map.
    /// If there are no instances left nothing is done.
    /// </summary>
    [DataField]
    public ProtoId<PreloadedGridPrototype>? PreloadedGrid;
}

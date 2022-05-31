using Content.Server.Maps;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Configurations;

/// <summary>
/// A generic configuration, for game rules that don't have special config data.
/// </summary>
[UsedImplicitly]
public sealed class NukeopsGameRuleConfiguration : GameRuleConfiguration
{
    [DataField("id", required: true)]
    private string _id = default!;
    public override string Id => _id;

    /// <summary>
    /// List of species that nukies can be
    /// </summary>
    /// <remarks>
    /// Yes, you can put anything in here. Including bread.
    /// </remarks>
    [DataField("pickableSpecies")]
    public List<string>? Species;

    /// <summary>
    /// Shuttles the nukies can spawn on
    /// </summary>
    [DataField("shuttles")]
    public List<GameMapPrototype>? Shuttles;

    /// <summary>
    /// Loadouts for different "professions" of nukie (commander, medic)
    /// </summary>
    [DataField("loadouts")]
    public Dictionary<string, List<StartingGearPrototype>>? Loadouts;
}

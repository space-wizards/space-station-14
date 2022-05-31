using Content.Server.Maps;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

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
    [DataField("pickableSpecies")]
    public List<string>? Species;

    /// <summary>
    /// Shuttles the nukies can spawn on
    /// </summary>
    [DataField("shuttles")]
    public List<string>? Shuttles;

    /// <summary>
    /// Loadouts for different "professions" of nukie (commander, medic)
    /// </summary>
    [DataField("loadouts")]
    public Dictionary<string, List<string>>? Loadouts;
}

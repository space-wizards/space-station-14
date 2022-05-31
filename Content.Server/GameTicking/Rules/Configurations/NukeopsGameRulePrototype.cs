using JetBrains.Annotations;

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
    public List<string> Species = new() { "MobHuman" };

    /// <summary>
    /// Shuttles the nukies can spawn on
    /// </summary>
    [DataField("shuttles")]
    public List<string> Shuttles = new() { "Maps/infiltrator.yml" };

    /// <summary>
    /// Loadouts for the commander
    /// </summary>
    [DataField("commanderLoadouts")]
    public List<string> CommanderLoadouts = new()
    {
        "SyndicateCommanderGearFull"
    };

    /// <summary>
    /// Loadouts for medical operatives
    /// </summary>
    [DataField("medicLoadouts")]
    public List<string> MedicLoadouts = new()
    {
        "SyndicateOperativeMedicFull"
    };

    /// <summary>
    /// Loadouts for regular operatives
    /// </summary>
    [DataField("operativeLoadouts")]
    public List<string> OperativeLoadouts = new()
    {
        "SyndicateOperativeGearFull"
    };
}

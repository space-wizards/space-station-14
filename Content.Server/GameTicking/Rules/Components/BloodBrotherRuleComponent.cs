namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(BloodBrotherRuleSystem))]
public sealed partial class BloodBrotherRuleComponent : Component
{
    public readonly List<EntityUid> Minds = new();
    public static readonly List<EntityUid> CommonObjectives = new();

    /// <summary>
    /// Minimal amount of bros created.
    /// </summary>
    [DataField]
    public int MinBros = 2;

    /// <summary>
    /// Max amount of bros created.
    /// </summary>
    [DataField]
    public int MaxBros = 3;

    /// <summary>
    /// Max amount of objectives possible.
    /// </summary>
    [DataField]
    public int MaxObjectives = 6;
}

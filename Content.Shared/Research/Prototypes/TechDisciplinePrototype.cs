using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Research.Prototypes;

/// <summary>
/// This is a prototype for a research discipline, a category
/// that governs how <see cref="TechnologyPrototype"/>s are unlocked.
/// </summary>
[Prototype("techDiscipline")]
public sealed class TechDisciplinePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Player-facing name.
    /// Supports locale strings.
    /// </summary>
    [DataField("name", required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// A color used for UI
    /// </summary>
    [DataField("color", required: true)]
    public Color Color;

    /// <summary>
    /// An icon used to visually represent the discipline in UI.
    /// </summary>
    [DataField("icon")]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// For each tier a discipline supports, what percentage
    /// of the previous tier must be unlocked for it to become available
    /// </summary>
    [DataField("tierPrerequisites", required: true)]
    public Dictionary<int, float> TierPrerequisites = new();

    /// <summary>
    /// Purchasing this tier of technology causes a server to become "locked" to this discipline.
    /// </summary>
    [DataField("lockoutTier")]
    public int LockoutTier = 3;
}

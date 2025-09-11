using Content.Shared.Construction.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Conditions;

/// <summary>
///   A check to see if the entity itself can be crafted.
/// </summary>
[DataDefinition]
public sealed partial class EntityWhitelistCondition : IConstructionCondition
{
    /// <summary>
    /// What is told to the player attempting to construct the recipe using this condition. This will be localised.
    /// </summary>
    [DataField("conditionString")]
    public string ConditionString = "construction-step-condition-entity-whitelist";

    /// <summary>
    /// The icon shown to the player beside the condition string.
    /// </summary>
    [DataField("conditionIcon")]
    public SpriteSpecifier? ConditionIcon = null;

    /// <summary>
    /// The whitelist that allows only certain entities to use this.
    /// </summary>
    [DataField("whitelist", required: true)]
    public EntityWhitelist Whitelist = new();

    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        var whitelistSystem = IoCManager.Resolve<IEntityManager>().System<EntityWhitelistSystem>();
        return whitelistSystem.IsWhitelistPass(Whitelist, user);
    }

    public ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = ConditionString,
            Icon = ConditionIcon
        };
    }
}

using Content.Shared.Construction.EntitySystems;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Shared.Construction.Conditions;

/// <summary>
///   Check for "Unstackable" condition commonly used by atmos devices and others which otherwise don't check on
///   collisions with other items.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed class NoUnstackableInTile : IConstructionCondition
{
    public const string GuidebookString = "construction-step-condition-no-unstackable-in-tile";
    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        var anchorable = IoCManager.Resolve<SharedAnchorableSystem>();

        return !anchorable.AnyUnstackablesAnchoredAt(location);
    }

    public ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = GuidebookString
        };
    }
}

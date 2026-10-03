using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using YamlDotNet.Core.Tokens;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Conditions
{
    /// <summary>
    ///     This condition checks whether if an entity with the <see cref="TagComponent"/> possesses a specific tag
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class HasTag : GraphConditionBase<IHasTagCondition>, IHasTagCondition
    {
        /// <summary>
        ///     The tag the entity is being checked for
        /// </summary>
        [DataField("tag")]
        public ProtoId<TagPrototype> Tag { get; private set; }

        public override bool DoExamine(ExaminedEvent args)
        {
            return false;
        }

        public override IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
            };
        }
    }
}

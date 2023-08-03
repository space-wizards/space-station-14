using System.Linq;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class CrafterWhitelist : IConstructionCondition
    {
        [DataField("whitelist", required: true)]
        public EntityWhitelist Whitelist = new();

        /// <summary>
        /// It's hard to think of variable names at 10 pm. This should explain the whitelist in simple terms
        /// (So instead of saying "You need this component and this tag", you can say "You have to be this")
        /// </summary>
        [DataField("text")]
        public string Text = "construction-step-condition-crafter-whitelist";

        public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
        {
            if (Whitelist.IsValid(user))
                return true;

            return false;
        }
        public ConstructionGuideEntry GenerateGuideEntry()
        {
            return new ConstructionGuideEntry
            {
                Localization = Text,
            };
        }
    }
}

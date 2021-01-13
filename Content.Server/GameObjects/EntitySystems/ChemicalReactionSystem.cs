using Content.Shared.Chemistry;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.NewFolder
{
    public class ChemicalReactionSystem : SharedChemicalReactionSystem
    {
        protected override void OnReaction(ReactionPrototype reaction, IEntity owner, ReagentUnit unitReactions)
        {
            base.OnReaction(reaction, owner, unitReactions);

            Get<AudioSystem>().PlayAtCoords(reaction.Sound, owner.Transform.Coordinates);
        }
    }
}

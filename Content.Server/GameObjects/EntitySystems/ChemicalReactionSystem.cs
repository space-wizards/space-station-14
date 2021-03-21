#nullable enable
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.GameObjects.EntitySystems.NewFolder
{
    public class ChemicalReactionSystem : SharedChemicalReactionSystem
    {
        protected override void OnReaction(ReactionPrototype reaction, IEntity owner, ReagentUnit unitReactions)
        {
            base.OnReaction(reaction, owner, unitReactions);

            if (reaction.Sound != null)
                SoundSystem.Play(Filter.Pvs(owner), reaction.Sound, owner.Transform.Coordinates);
        }
    }
}

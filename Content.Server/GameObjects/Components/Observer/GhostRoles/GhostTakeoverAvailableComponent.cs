using System;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Players;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Observer.GhostRoles
{
    /// <summary>
    ///     Allows a ghost to take over the Owner entity.
    /// </summary>
    [RegisterComponent, ComponentReference(typeof(GhostRoleComponent))]
    public class GhostTakeoverAvailableComponent : GhostRoleComponent
    {
        public override string Name => "GhostTakeoverAvailable";

        public override bool Take(IPlayerSession session)
        {
            if (Taken)
                return false;

            Taken = true;

            var mind = Owner.EnsureComponent<MindComponent>();

            if(mind.HasMind)
                throw new Exception("MindComponent already has a mind!");

            session.ContentData().Mind.TransferTo(Owner);

            EntitySystem.Get<GhostRoleSystem>().UnregisterGhostRole(this);

            return true;
        }
    }
}

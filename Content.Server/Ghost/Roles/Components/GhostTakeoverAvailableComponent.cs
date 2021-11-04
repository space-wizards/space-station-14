using System;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.Ghost.Roles.Components
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

            if (mind.HasMind)
                return false;

            var sessionMind = session.ContentData()?.Mind;

            DebugTools.AssertNotNull(sessionMind);

            sessionMind!.TransferTo(Owner);

            EntitySystem.Get<GhostRoleSystem>().UnregisterGhostRole(this);

            return true;
        }
    }
}

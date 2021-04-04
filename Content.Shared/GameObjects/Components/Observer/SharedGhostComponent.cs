using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Observer
{
    public class SharedGhostComponent : Component, IActionBlocker
    {
        public override string Name => "Ghost";
        public override uint? NetID => ContentNetIDs.GHOST;

        public bool CanInteract() => false;
        public bool CanUse() => false;
        public bool CanThrow() => false;
        public bool CanDrop() => false;
        public bool CanPickup() => false;
        public bool CanEmote() => false;
        public bool CanAttack() => false;
    }

    [Serializable, NetSerializable]
    public class GhostComponentState : ComponentState
    {
        public bool CanReturnToBody { get; }

        public GhostComponentState(bool canReturnToBody) : base(ContentNetIDs.GHOST)
        {
            CanReturnToBody = canReturnToBody;
        }
    }

    [Serializable, NetSerializable]
    public class ReturnToBodyComponentMessage : ComponentMessage
    {
        public ReturnToBodyComponentMessage()
        {
            Directed = true;
        }
    }

    [Serializable, NetSerializable]
    public class GhostRequestWarpPointData : ComponentMessage
    {
        public GhostRequestWarpPointData()
        {
            Directed = true;
        }
    }

    [Serializable, NetSerializable]
    public class GhostRequestPlayerNameData : ComponentMessage
    {
        public GhostRequestPlayerNameData()
        {
            Directed = true;
        }
    }

    [Serializable, NetSerializable]
    public class GhostReplyWarpPointData : ComponentMessage
    {
        public List<string> WarpName;

        public GhostReplyWarpPointData(List<string> warpName)
        {
            WarpName = warpName;
            Directed = true;
        }
    }

    [Serializable, NetSerializable]
    public class GhostReplyPlayerNameData : ComponentMessage
    {
        public Dictionary<EntityUid,string> PlayerNames;

        public GhostReplyPlayerNameData(Dictionary<EntityUid, string> playerNameDict)
        {
            PlayerNames = playerNameDict;
            Directed = true;
        }
    }
}



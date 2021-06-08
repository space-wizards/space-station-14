using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Observer
{
    public class SharedGhostComponent : Component, IActionBlocker
    {
        public override string Name => "Ghost";
        public override uint? NetID => ContentNetIDs.GHOST;

        [DataField("canReturnToBody")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanReturnToBody { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public HashSet<string> LocationWarps { get; set; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        public Dictionary<EntityUid, string> PlayerWarps { get; set; } = new();

        public override ComponentState GetComponentState(ICommonSession player)
        {
            if (player.AttachedEntity != Owner)
            {
                return new GhostComponentState(false);
            }

            return new GhostComponentState(CanReturnToBody, LocationWarps, PlayerWarps);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not GhostComponentState state)
            {
                return;
            }

            CanReturnToBody = state.CanReturnToBody;
            LocationWarps = state.LocationWarps ?? new HashSet<string>();
            PlayerWarps = state.PlayerWarps ?? new Dictionary<EntityUid, string>();
        }

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

        public HashSet<string>? LocationWarps { get; }

        public Dictionary<EntityUid, string>? PlayerWarps { get; }

        public GhostComponentState(
            bool canReturnToBody,
            HashSet<string>? locationWarps = null,
            Dictionary<EntityUid, string>? playerWarps = null)
            : base(ContentNetIDs.GHOST)
        {
            CanReturnToBody = canReturnToBody;
            LocationWarps = locationWarps;
            PlayerWarps = playerWarps;
        }
    }
}



using System;
using System.Collections.Generic;
using Content.Shared.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Ghost
{
    [NetworkedComponent()]
    public class SharedGhostComponent : Component, IActionBlocker
    {
        public override string Name => "Ghost";

        /// <summary>
        ///     Changed by <see cref="GhostChangeCanReturnToBodyEvent"/>
        /// </summary>
        [DataField("canReturnToBody")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanReturnToBody { get; set; }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new GhostComponentState(CanReturnToBody);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not GhostComponentState state)
            {
                return;
            }

            CanReturnToBody = state.CanReturnToBody;
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
        {
            CanReturnToBody = canReturnToBody;
            LocationWarps = locationWarps;
            PlayerWarps = playerWarps;
        }
    }
}



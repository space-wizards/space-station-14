#nullable enable
using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cuffs.Components
{
    [NetID(ContentNetIDs.HANDCUFFS)]
    public abstract class SharedHandcuffComponent : Component
    {
        public override string Name => "Handcuff";

        [Serializable, NetSerializable]
        protected sealed class HandcuffedComponentState : ComponentState
        {
            public string? IconState { get; }

            public HandcuffedComponentState(string? iconState)
            {
                IconState = iconState;
            }
        }
    }
}

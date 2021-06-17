#nullable enable
using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Tag
{
    [Serializable, NetSerializable]
    public class TagComponentState : ComponentState
    {
        public TagComponentState(string[] tags) : base(ContentNetIDs.TAG)
        {
            Tags = tags;
        }

        public string[] Tags { get; }
    }
}

#nullable enable
using Robust.Shared.GameObjects;
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Tag
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

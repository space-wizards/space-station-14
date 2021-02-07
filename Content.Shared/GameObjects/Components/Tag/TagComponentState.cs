#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Tag
{
    public class TagComponentState : ComponentState
    {
        public TagComponentState(string[] tags) : base(ContentNetIDs.TAG)
        {
            Tags = tags;
        }

        public string[] Tags { get; }
    }
}

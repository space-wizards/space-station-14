using Robust.Shared.Serialization;

namespace Content.Shared.Tag
{
    [Serializable, NetSerializable]
    public sealed partial class TagComponentState : ComponentState
    {
        public TagComponentState(string[] tags)
        {
            Tags = tags;
        }

        public string[] Tags { get; }
    }
}

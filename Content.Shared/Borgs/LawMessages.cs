using Robust.Shared.Serialization;

namespace Content.Shared.Borgs
{
    [NetSerializable, Serializable]
    public enum LawsUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class LawsUpdateState : BoundUserInterfaceState
    {
        public HashSet<string> Laws;

        public LawsUpdateState(HashSet<string> laws)
        {
            Laws = laws;
        }
    }

    /// <summary>
    ///     Ask server to state our laws.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class StateLawsMessage : BoundUserInterfaceMessage
    {
        public StateLawsMessage()
        {}
    }

    [Serializable, NetSerializable]
    public sealed class LawsBoundInterfaceState : BoundUserInterfaceState
    {}
}

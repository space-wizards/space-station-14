using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public record GameRuleInfo(
        NetEntity? Entity,
        string Name);
}

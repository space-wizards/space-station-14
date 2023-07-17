using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Events
{
    [Serializable, NetSerializable]
    public sealed class GameRulesListEvent : EntityEventArgs
    {
        public List<GameRuleInfo> ActiveGameRules = new();
    }
}

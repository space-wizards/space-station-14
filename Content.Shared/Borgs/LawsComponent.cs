using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Borgs
{
    [RegisterComponent, NetworkedComponent]
    [Access(typeof(SharedLawsSystem))]
    public sealed class LawsComponent : Component
    {
        [DataField("initialLaws")]
        public List<string> InitialLaws = new List<string>();
        public SortedDictionary<int, (string Text, LawProperties Properties)> Laws = new SortedDictionary<int, (string, LawProperties)>();

        [DataField("canState")]
        public bool CanState = true;

        /// <summary>
        ///     Antispam.
        /// </summary>
        public TimeSpan? StateTime = null;

        [DataField("stateCD")]
        public TimeSpan StateCD = TimeSpan.FromSeconds(30);
    }

    [Serializable, NetSerializable]
    public sealed class LawsComponentState : ComponentState
    {
        public Dictionary<int, (string, LawProperties)> Laws;

        public LawsComponentState(Dictionary<int, (string, LawProperties)> laws)
        {
            Laws = laws;
        }
    }
    [Flags]
    public enum LawProperties
    {
        Default = 0,

        /// <summary>
        /// Whether this law should be skipped when stating laws
        /// and other law inspection methods.
        /// </summary>
        Hidden = 1 << 0,

        /// <summary>
        /// Whether this law can be removed by normal means.
        /// </summary>
        Removable = 1 << 1,
    }
}

using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    /// <summary>
    /// For shared behavior between chemical analysis machines
    /// </summary>
    public sealed class ChemAnalyserComponent : Component
    {
        /// <summary>
        /// What method of input the machine will interact with
        /// </summary>
        [DataField("machineInputDevice", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: false)]
        public string MachineInputDevice = string.Empty;

        /// <summary>
        /// ChemAnalyser runtime and tracking thereof
        /// </summary>
        [DataField("delay")]
        public float Delay = 5f;
        [ViewVariables]
        [DataField("accumulator")]
        public float Accumulator = 0f;

        /// <summary>
        /// What the machine will spawn
        /// </summary>
        [DataField("machineOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
        public string MachineOutput = string.Empty;

        //TODO it may be worth separating the reward conditions as a separate component so the same machine can support multiple rewards
        //for now keep it as is, but doing the above may allow the same machine to support multiple research tiers
        /// <summary>
        /// What the machine will spawn when the reward condition is met (if both are provided)
        /// </summary>
        [DataField("researchDiskReward", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: false)]
        public string ResearchDiskReward = string.Empty;

        /// <summary>
        /// Number of reagents that will trigger the research disk reward (if above 0) mutually exclusive with other reward conditions
        /// </summary>
        [ViewVariables]
        [DataField("reagentRewardCount")]
        public int ReagentRewardCount = 0;

        /// <summary>
        /// Reagents required that will trigger the research disk reward (if present) mutually exclusive and overrides with other reward conditions
        /// </summary>
        [ViewVariables]
        [DataField("reagentsRewardRequiredNames")]
        public readonly List<string> ReagentsRewardRequiredNames = new();

        /// <summary>
        /// Group reagents must have to count towards the reagentRewardCount
        /// </summary>
        [ViewVariables]
        [DataField("reagentRewardRequiredGroupFilter")]
        public string ReagentRewardRequiredGroupFilter = string.Empty;

        /// <summary>
        /// Reagents that will not count toward the reagentRewardCount by name
        /// </summary>
        [ViewVariables]
        [DataField("reagentRewardExcludedNamesFilter")]
        public readonly List<string> ReagentRewardExcludedNamesFilter = new();

        /// <summary>
        /// Reagents that will not count toward the reagentRewardCount by group
        /// </summary>
        [ViewVariables]
        [DataField("reagentRewardExcludedGroupsFilter")]
        public readonly List<string> ReagentRewardExcludedGroupsFilter = new();

        /// <summary>
        /// Reagents that will not be displayed by group
        /// </summary>
        [ViewVariables]
        [DataField("reagentDisplayExcludedGroupsFilter")]
        public readonly List<string> ReagentDisplayExcludedGroupsFilter = new();

        /// <summary>
        /// Groups reagents must have to be displayed
        /// </summary>
        [ViewVariables]
        [DataField("reagentDisplayRequiredGroupFilter")]
        public string ReagentDisplayRequiredGroupFilter = string.Empty;

        /// <summary>
        /// Reagents that will not be displayed by name
        /// </summary>
        [ViewVariables]
        [DataField("reagentDisplayExcludedNamesFilter")]
        public readonly List<string> ReagentDisplayExcludedNamesFilter = new();

        /// <summary>
        /// The printer checks this list with the one it has - if the reagents are identical (quantity disregarded) the machine will not print
        /// </summary>
        [ViewVariables]
        public List<string> LastRecordedReagentSet = new();
        [ViewVariables]
        public bool DiskPrinted = false;
    }
}

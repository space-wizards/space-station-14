using Content.Server.AI.Systems;

namespace Content.Server.AI.Components
{
    [Access(typeof(NPCSystem))]
    public abstract class NPCComponent : Component
    {
        /// <summary>
        /// Contains all of the world data for a particular NPC in terms of how it sees the world.
        /// </summary>
        [ViewVariables, DataField("blackboard")]
        public Dictionary<string, object> BlackboardA = new()
        {
            { "visionRadius", 7f }
        };

        public float VisionRadius => (float) BlackboardA["visionRadius"];
    }
}

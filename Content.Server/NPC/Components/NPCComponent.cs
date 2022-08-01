namespace Content.Server.NPC.Components
{
    public abstract class NPCComponent : Component
    {
        /// <summary>
        /// Contains all of the world data for a particular NPC in terms of how it sees the world.
        /// </summary>
        [ViewVariables, DataField("blackboard")]
        public NPCBlackboard BlackboardA = new();

        public float VisionRadius => BlackboardA.GetValue<float>("visionRadius");
    }
}

using Content.Server.AI.EntitySystems;

namespace Content.Server.AI.Components
{
    [RegisterComponent, Access(typeof(NPCSystem))]
    [Virtual]
    public class NPCComponent : Component
    {
        /// <summary>
        /// Contains all of the world data for a particular NPC in terms of how it sees the world.
        /// </summary>
        [ViewVariables, DataField("blackboard")]
        public Dictionary<string, object> BlackboardA = new();
    }
}

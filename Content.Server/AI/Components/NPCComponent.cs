using Content.Server.AI.EntitySystems;

namespace Content.Server.AI.Components
{
    [Access(typeof(NPCSystem))]
    public abstract class NPCComponent : Component
    {
        // TODO: Soon. I didn't realise how much effort it was to deprecate the old one.
        /// <summary>
        /// Contains all of the world data for a particular NPC in terms of how it sees the world.
        /// </summary>
        //[ViewVariables, DataField("blackboardA")]
        //public Dictionary<string, object> BlackboardA = new();

        public float VisionRadius => 7f;
    }
}

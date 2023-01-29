using Content.Shared.DragDrop;
using Content.Shared.Storage;

namespace Content.Shared.Nutrition.Components
{
    /// <summary>
    /// Indicates that the entity can be thrown on a kitchen spike for butchering.
    /// </summary>
    [RegisterComponent]
    public sealed class SharedButcherableComponent : Component, IDraggable
    {
        [DataField("spawned", required: true)]
        public List<EntitySpawnEntry> SpawnedEntities = new();

        [DataField("butcherDelay")]
        public float ButcherDelay = 8.0f;

        [DataField("butcheringType")]
        public ButcheringType Type = ButcheringType.Knife;

        /// <summary>
        /// Prevents butchering same entity on two and more spikes simultaneously and multiple doAfters on the same Spike
        /// </summary>
        public bool BeingButchered;

        // TODO: ECS this out!, my guess CanDropEvent should be client side only and then "ValidDragDrop" in the DragDropSystem needs a little touch
        // But this may lead to creating client-side systems for every Draggable component subbed to CanDrop. Actually those systems could control
        // CanDropOn behaviors as well (IDragDropOn)
        bool IDraggable.CanDrop(CanDropEvent args)
        {
            return Type != ButcheringType.Knife;
        }
    }

    public enum ButcheringType
    {
        Knife, // e.g. goliaths
        Spike, // e.g. monkeys
        Gibber // e.g. humans. TODO
    }
}

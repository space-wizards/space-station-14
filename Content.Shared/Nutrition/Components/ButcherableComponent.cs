using Content.Shared.Storage;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components
{
    /// <summary>
    /// Indicates that the entity can be thrown on a kitchen spike for butchering.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class ButcherableComponent : Component
    {
        [DataField("spawned", required: true)]
        public List<EntitySpawnEntry> SpawnedEntities = new();

        [ViewVariables(VVAccess.ReadWrite), DataField("butcherDelay")]
        public float ButcherDelay = 8.0f;

        [ViewVariables(VVAccess.ReadWrite), DataField("butcheringType")]
        public ButcheringType Type = ButcheringType.Knife;

        /// <summary>
        /// Prevents butchering same entity on two and more spikes simultaneously and multiple doAfters on the same Spike
        /// </summary>
        [ViewVariables]
        public bool BeingButchered;
    }

    public enum ButcheringType : byte
    {
        Knife, // e.g. goliaths
        Spike, // e.g. monkeys
        Gibber // e.g. humans. TODO
    }
}

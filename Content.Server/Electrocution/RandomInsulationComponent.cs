using Content.Shared.Electrocution;

namespace Content.Server.Electrocution
{
    /// <summary>
    /// An entity with this component will assign a random siemens coefficient on the entities insulated component (if it exists)
    /// Mostly used to randomize budget insulated gloves.
    /// </summary>
    [RegisterComponent]
    public sealed partial class RandomInsulationComponent : Component
    {
        /// <summary>
        /// A list of possible Siemens Factors from which one will be picked for insulation component.
        /// </summary>
        /// <seealso cref="InsulatedComponent.Coefficient"/>
        [DataField("list")]
        public float[] List = { 0f };
    }
}

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Electrocution
{
    // Technically, people could cheat and figure out which budget insulated gloves are gud and which ones are bad.
    // We might want to rethink this a little bit.
    [Access(typeof(SharedElectrocutionSystem))]
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed class InsulatedComponent : Component
    {
        /// <summary>
        ///     Siemens coefficient. Zero means completely insulated.
        /// </summary>
        [DataField("coefficient")]
        [AutoNetworkedField]
        public float SiemensCoefficient { get; set; } = 0f;
    }
}

using Robust.Shared.GameStates;

namespace Content.Shared.Electrocution
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(SharedElectrocutionSystem))]
    public sealed partial class InsulatedComponent : Component
    {
        // Technically, people could cheat and figure out which budget insulated gloves are gud and which ones are bad.
        // We might want to rethink this a little bit.
        /// <summary>
        ///     Siemens coefficient. Zero means completely insulated.
        /// </summary>
        [DataField, AutoNetworkedField]
        public float Coefficient { get; set; } = 0f;
    }
}

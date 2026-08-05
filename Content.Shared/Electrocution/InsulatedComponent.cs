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

        // DS14-start
        [DataField, AutoNetworkedField]
        public bool ShowInExamine = true;

        [DataField, AutoNetworkedField]
        public float LightningProtectionChance = 0f;

        [DataField, AutoNetworkedField]
        public TimeSpan StunReduction = TimeSpan.FromSeconds(1);
        // DS14-end
    }
}

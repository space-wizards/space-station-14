using Content.Shared.Atmos;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Lathe
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class LatheGasComponent : Component
    {
        /// <summary>
        ///     The ID for the pipe node.
        /// </summary>
        [DataField]
        public string Inlet = "pipe";

        /// <summary>
        ///     Id for gas that will be consumed by lathe.
        /// </summary>
        [DataField(required: true)]
        public Gas GasId;

        /// <summary>
        ///     Amount of moles that will be consumed by lathe
        /// </summary>
        [DataField(required: true)]
        public float GasAmount;
    }
}

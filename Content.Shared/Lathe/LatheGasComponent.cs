using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared.Lathe
{
    /// <summary>
    ///     Component that force lathe to use gas during processing.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class GasForProducingComponent : Component
    {
        /// <summary>
        ///     The ID for the pipe node.
        /// </summary>
        [DataField]
        public string Inlet = "pipe";

        /// <summary>
        ///     Id for gas that will be consumed by lathe.
        /// </summary>
        [DataField(required: true), AutoNetworkedField]
        public Gas GasId;

        /// <summary>
        ///     Amount of moles that will be consumed by lathe per recipe.
        /// </summary>
        [DataField(required: true), AutoNetworkedField]
        public float GasAmount;
    }
}

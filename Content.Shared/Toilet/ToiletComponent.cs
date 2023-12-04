using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Toilet
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ToiletComponent : Component
    {
        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier? Sound;

        [DataField, AutoNetworkedField]
        public bool ToggleSeat;

        /// <summary>
        /// The state for when the toilet seat is up.
        /// </summary>
        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public string? SeatUp;

        /// <summary>
        /// The state for when the toilet seat is down.
        /// </summary>
        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public string? SeatDown;
    }
}

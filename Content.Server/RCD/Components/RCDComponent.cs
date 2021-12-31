using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.RCD.Components
{
    public enum RcdMode
    {
        Floors,
        Walls,
        Airlock,
        Deconstruct
    }

    [RegisterComponent]
    public class RCDComponent : Component
    {
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("maxAmmo")] public int MaxAmmo = 5;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("startingAmmo")] public int StartingAmmo = 5;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("delay")]
        public float Delay = 2f;

        [DataField("swapModeSound")]
        public SoundSpecifier SwapModeSound = new SoundPathSpecifier("/Audio/Items/genhit.ogg");

        [DataField("successSound")]
        public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");

        /// <summary>
        ///     What mode are we on? Can be floors, walls, deconstruct.
        /// </summary>
        public RcdMode Mode = RcdMode.Floors;

        /// <summary>
        ///     How much "ammo" we have left. You can refill this with RCD ammo.
        /// </summary>
        public int CurrentAmmo;

    }
}

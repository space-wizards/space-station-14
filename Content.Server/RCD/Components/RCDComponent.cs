using System.Threading;
using Robust.Shared.Audio;

namespace Content.Server.RCD.Components
{
    public enum RcdMode : byte
    {
        Floors,
        Walls,
        Airlock,
        Deconstruct
    }

    [RegisterComponent]
    public sealed class RCDComponent : Component
    {
        private const int DefaultAmmoCount = 5;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("maxAmmo")] public int MaxAmmo = DefaultAmmoCount;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("delay")]
        public float Delay = 2f;

        [DataField("swapModeSound")]
        public SoundSpecifier SwapModeSound = new SoundPathSpecifier("/Audio/Items/genhit.ogg");

        [DataField("successSound")]
        public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");

        /// <summary>
        ///     What mode are we on? Can be floors, walls, deconstruct.
        /// </summary>
        [DataField("mode")]
        public RcdMode Mode = RcdMode.Floors;

        /// <summary>
        ///     How much "ammo" we have left. You can refill this with RCD ammo.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("ammo")]
        public int CurrentAmmo = DefaultAmmoCount;

        public CancellationTokenSource? CancelToken = null;
    }
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.SmartFridge
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class SmartFridgeComponent : Component
    {
        /// <summary>
        /// Used by the server to determine how long the vending machine stays in the "Deny" state.
        /// Used by the client to determine how long the deny animation should be played.
        /// </summary>
        [DataField("denyDelay")]
        public float DenyDelay = 2.0f;

        public bool Denying;
        public bool Broken;

        /// <summary>
        ///     Sound that plays when an item can't be ejected
        /// </summary>
        [DataField("soundDeny")]
        // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
        public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

        #region Client Visuals
        /// <summary>
        /// RSI state for when the vending machine is unpowered.
        /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.Base"/>
        /// </summary>
        [DataField("offState")]
        public string? OffState;

        /// <summary>
        /// RSI state for the screen of the vending machine
        /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.Screen"/>
        /// </summary>
        [DataField("screenState")]
        public string? ScreenState;

        /// <summary>
        /// RSI state for the vending machine's normal state. Usually a looping animation.
        /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.BaseUnshaded"/>
        /// </summary>
        [DataField("normalState")]
        public string? NormalState;

        /// <summary>
        /// RSI state for the vending machine's deny animation. Will either be played once as sprite flick
        /// or looped depending on how <see cref="LoopDenyAnimation"/> is set.
        /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.BaseUnshaded"/>
        /// </summary>
        [DataField("denyState")]
        public string? DenyState;

        /// <summary>
        /// RSI state for when the vending machine is unpowered.
        /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.Base"/>
        /// </summary>
        [DataField("brokenState")]
        public string? BrokenState;

        /// <summary>
        /// If set to <c>true</c> (default) will loop the animation of the <see cref="DenyState"/> for the duration
        /// of <see cref="VendingMachineComponent.DenyDelay"/>. If set to <c>false</c> will play a sprite
        /// flick animation for the state and then linger on the final frame until the end of the delay.
        /// </summary>
        [DataField("loopDeny")]
        public bool LoopDenyAnimation = true;
        #endregion
    }

    [Serializable, NetSerializable]
    public enum SmartFridgeVisuals
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public enum SmartFridgeVisualState
    {
        Normal,
        Off,
        Broken,
        Deny,
    }

    [Serializable, NetSerializable]
    public enum SmartFridgeVisualLayers : byte
    {
        /// <summary>
        /// Off / Broken. The other layers will overlay this if the machine is on.
        /// </summary>
        Base,
        /// <summary>
        /// Normal / Deny / Eject
        /// </summary>
        BaseUnshaded,
        /// <summary>
        /// Screens that are persistent (where the machine is not off or broken)
        /// </summary>
        Screen
    }

    [Serializable, NetSerializable]
    public enum SmartFridgeUiKey
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class SmartFridgeInteractWithItemEvent : BoundUserInterfaceMessage
    {
        public readonly NetEntity InteractedItemUID;
        public SmartFridgeInteractWithItemEvent(NetEntity interactedItemUID)
        {
            InteractedItemUID = interactedItemUID;
        }
    }
}

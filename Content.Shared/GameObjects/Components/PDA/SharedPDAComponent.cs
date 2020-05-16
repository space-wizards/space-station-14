using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.PDA
{
    public class SharedPDAComponent : Component
    {
        public override string Name => "PDA";
        public override uint? NetID => ContentNetIDs.PDA;

        public override void Initialize()
        {
            base.Initialize();
        }


    }

    [Serializable, NetSerializable]
    public sealed class PDAToggleFlashlightMessage : BoundUserInterfaceMessage
    {
        public PDAToggleFlashlightMessage()
        {

        }
    }

    [Serializable, NetSerializable]
    public sealed class PDAEjectIDMessage : BoundUserInterfaceMessage
    {
        public PDAEjectIDMessage()
        {

        }
    }

    [Serializable, NetSerializable]
    public sealed class PDAUpdateUserInterfaceState : BoundUserInterfaceState
    {
        public bool FlashlightEnabled;
        public PDAIdInfoText PDAOwnerInfo;


        public PDAUpdateUserInterfaceState(bool isFlashlightOn, PDAIdInfoText ownerInfo)
        {
            FlashlightEnabled = isFlashlightOn;
            PDAOwnerInfo = ownerInfo;
        }
    }

    [NetSerializable, Serializable]
    public struct PDAIdInfoText
    {
        public string ActualOwnerName;
        public string IDOwner;
        public string JobTitle;
    }

    [NetSerializable, Serializable]
    public enum PDAVisuals
    {
        ScreenLit,
    }

    [NetSerializable, Serializable]
    public enum PDAUiKey
    {
        Key
    }
}

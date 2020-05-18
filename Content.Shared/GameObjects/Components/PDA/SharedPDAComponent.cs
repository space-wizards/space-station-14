using System;
using System.Collections.Generic;
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

    [Serializable, NetSerializable]
    public sealed class PDASendUplinkListingsMessage : BoundUserInterfaceState
    {

        public IReadOnlyList<UplinkStoreListing> Listings;
        public PDASendUplinkListingsMessage(IReadOnlyList<UplinkStoreListing> listings)
        {
            Listings = listings;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PDARequestUplinkListingsMessage : BoundUserInterfaceMessage
    {
        public PDARequestUplinkListingsMessage()
        {

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

    public struct UplinkAccount
    {
        public EntityUid AccountHolder;
        public int Balance;
    }

    [Serializable] [NetSerializable]
    public struct UplinkStoreListing
    {
        public EntityPrototype Item;
        public int Price;
        public string Category;
        public string Description;
    }
}

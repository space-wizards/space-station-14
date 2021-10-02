using System;
using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.RCD.Components
{
    [RegisterComponent]
    public class RCDAmmoComponent : Component
    {
        public override string Name => "RCDAmmo";

        //How much ammo we refill
        [ViewVariables(VVAccess.ReadWrite)] [DataField("refillAmmo")] public int RefillAmmo = 5;
    }
}

using System;
using System.Collections.Generic;
using Content.Server.Access.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.PDA
{
    [RegisterComponent]
    public class PDAComponent : Component
    {
        public override string Name => "PDA";

        [DataField("idSlot")] 
        public string IdSlot = "pdaIdSlot";

        [DataField("penSlot")]
        public string PenSlot = "pdaPenSlot";

        [ViewVariables] [DataField("idCard")] public string? StartingIdCard;

        [ViewVariables] public IdCardComponent? ContainedID;
        [ViewVariables] public bool PenInserted;
        [ViewVariables] public bool FlashlightOn;

        [ViewVariables] public string? OwnerName;
    }
}

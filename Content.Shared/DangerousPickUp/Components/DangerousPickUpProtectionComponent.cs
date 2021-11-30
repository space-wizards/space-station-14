using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.DangerousPickUp.Components
{
    [RegisterComponent]
    public class DangerousPickUpProtectionComponent : Component
    {
        public override string Name => "DangerousPickUpProtection";
        
        [DataField("protectionType")]
        public string protectionType = string.Empty;
    }
}

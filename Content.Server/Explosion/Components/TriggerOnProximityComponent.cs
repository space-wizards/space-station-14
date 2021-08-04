using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    class TriggerOnProximityComponent : Component
    {
        public override string Name => "ProximityTrigger";

    }
}

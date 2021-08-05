using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    class TriggerOnProximityComponent : Component
    {
        public override string Name => "ProximityTrigger";

        public string ProximityFixture { get; } = "proximity-fixture";

        [DataField("shape", required: true)]
        public IPhysShape Shape { get; set; } = default!;

        public bool Enabled { get; set; } = true;

    }
}

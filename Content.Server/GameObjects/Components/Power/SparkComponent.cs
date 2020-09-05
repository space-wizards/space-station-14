using Content.Server.Atmos;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Interfaces.Random;
using System;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.IoC;
using YamlDotNet.Core.Tokens;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Server.GameObjects.EntitySystems;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    ///     Generic "spark" effect produced by things like hacking, RCDs, igniters, etc.
    ///
    ///     This is an entity rather than a particle because it needs to have behavior.
    /// </summary>
    [RegisterComponent]
    public class SparkComponent : Component
    {
        public override string Name => "Spark";
        public float AccumulatedFrameTime { get; private set; }
        public bool Ignite { get; set; } = true;

        public float Lifetime = 1.0f;

        public void Update(float frameTime)
        {
            AccumulatedFrameTime += frameTime;

            if (Ignite)
            {
                Owner.Transform.GridPosition.GetTileAtmosphere()?.HotspotExpose(700f, 50f, true);
                Ignite = false;
            }
        }
    }


}

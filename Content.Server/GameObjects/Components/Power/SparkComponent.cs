using Content.Server.Atmos;
using Robust.Shared.GameObjects;

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
                Owner.Transform.Coordinates.GetTileAtmosphere()?.HotspotExpose(700f, 50f, true);
                Ignite = false;
            }
        }
    }


}

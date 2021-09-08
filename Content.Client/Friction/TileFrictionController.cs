using Content.Client.Physics.Controllers;
using Content.Shared.Friction;
using Robust.Shared.GameObjects;

namespace Content.Client.Friction
{
    public sealed class TileFrictionController : SharedTileFrictionController
    {
        public override void Initialize()
        {
            base.Initialize();
            Mover = EntitySystem.Get<SharedPhysicsSystem>().GetController<MoverController>();
        }
    }
}

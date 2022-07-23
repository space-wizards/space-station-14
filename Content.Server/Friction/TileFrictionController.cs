using Content.Server.Physics.Controllers;
using Content.Shared.Friction;

namespace Content.Server.Friction
{
    public sealed class TileFrictionController : SharedTileFrictionController
    {
        public override void Initialize()
        {
            base.Initialize();
            Mover = Get<MoverController>();
        }
    }
}

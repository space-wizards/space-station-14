using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Decals
{
    public class DecalSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;

        public record Decal(EntityCoordinates position, string Id, Color? Color);

        public override void Initialize()
        {
            base.Initialize();


        }

    }
}

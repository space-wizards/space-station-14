#nullable enable
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using System;

namespace Content.Client.Graphics.Overlays
{
    public class ColoredScreenBorderOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _shader;

        public ColoredScreenBorderOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("ColoredScreenBorder").Instance();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var worldHandle = args.WorldHandle;
            worldHandle.UseShader(_shader);
            var viewport = _eyeManager.GetWorldViewport();
            worldHandle.DrawRect(viewport, Color.White);
        }
    }
}

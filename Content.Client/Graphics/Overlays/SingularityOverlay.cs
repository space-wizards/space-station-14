using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.Graphics.Clyde;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using OpenToolkit.Graphics.OpenGL4;
using Content.Shared.Interfaces;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Log;
using System;
using Robust.Shared.Utility;
using Robust.Client.Utility;
using Content.Client.GameObjects.Components.Mobs;
using System.Linq;
using Robust.Shared.Enums;

/*
namespace Content.Client.Graphics.Overlays
{
    public class SingularityOverlay : Overlay, IConfigurableOverlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClyde _displayManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        public override OverlayPriority Priority => OverlayPriority.P3;
        public override bool RequestScreenTexture => true;
        public override bool OverwriteTargetFrameBuffer => true;

        private readonly ShaderInstance _shader;
        private Vector2 _currentWorldCoords;
        private float _intensity = 3.8f;
        private float _falloff = 5.1f;

        public SingularityOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("Singularity").Instance().Duplicate();
        }

        protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
        {
            
            handle.UseShader(_shader);
            var worldHandle = (DrawingHandleWorld)handle;
            var viewport = _eyeManager.GetWorldViewport();

            var tempCoords = _eyeManager.WorldToScreen(_currentWorldCoords);
            tempCoords.Y = _displayManager.ScreenSize.Y - tempCoords.Y;
            _shader?.SetParameter("positionInput", tempCoords);
            if (ScreenTexture != null)
                _shader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _shader?.SetParameter("intensity", _intensity);
            _shader?.SetParameter("falloff", _falloff);
            worldHandle.DrawRect(viewport, Color.White);

        }

        public void Configure(OverlayParameter parameters)
        {
            if (parameters is KeyedVector2OverlayParameter kParams)
            {
                var dict = kParams.Dict;
                dict.TryGetValue("worldCoords", out _currentWorldCoords);
            }
            else if (parameters is KeyedFloatOverlayParameter fParams)
            {
                var dict = fParams.Dict;
                dict.TryGetValue("intensity", out _intensity);
                dict.TryGetValue("falloff", out _falloff);
            }
        }
    }
}
*/

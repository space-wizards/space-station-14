using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Graphics.Clyde;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using OpenToolkit.Graphics.OpenGL4;
using Content.Shared.Interfaces;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.Log;
using Robust.Shared.GameObjects.Components.Renderable;
using System;
using Robust.Shared.Utility;
using Robust.Client.Utility;
using Content.Client.GameObjects.Components.Mobs;
using System.Linq;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Enums;
using Robust.Client.Interfaces.Graphics;

namespace Content.Client.Graphics.Overlays
{
    public class SingularityOverlay : Overlay, IConfigurable<KeyedVector2OverlayParameter>, IConfigurable<KeyedFloatOverlayParameter>
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

        public SingularityOverlay() : base()
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

        public void Configure(KeyedVector2OverlayParameter parameters)
        {
            var dict = parameters.Dict;
            dict.TryGetValue("worldCoords", out _currentWorldCoords);
        }

        public void Configure(KeyedFloatOverlayParameter parameters)
        {
            var dict = parameters.Dict;
            dict.TryGetValue("intensity", out _intensity);
            dict.TryGetValue("falloff", out _falloff);
        }
    }
}

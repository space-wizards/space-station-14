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
    public class SingularityOverlay : Overlay, IConfigurable<PositionOverlayParameter>
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClyde _displayManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpaceFOVStencil;
        public override OverlayPriority Priority => OverlayPriority.P4;
        public override bool RequestScreenTexture => true;

        private readonly ShaderInstance _shader;
        private Vector2 _currentWorldCoords;

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
            if (ScreenTexture != null)
                _shader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            var tempCoords = _eyeManager.WorldToScreen(_currentWorldCoords);
            tempCoords.Y = Math.Abs(tempCoords.Y - _displayManager.ScreenSize.Y);
            _shader?.SetParameter("positionInput", tempCoords);
            worldHandle.DrawRect(viewport, Color.White);
        }

        public void Configure(PositionOverlayParameter parameters)
        {
            if (parameters.Positions.Length == 1)
                _currentWorldCoords = parameters.Positions[0];
            else
                Logger.Error("Error: {0} instead of 1 position parameter was sent to SingularityOverlay!", parameters.Positions.Length);
        }
    }
}

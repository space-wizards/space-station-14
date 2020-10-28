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

namespace Content.Client.Graphics.Overlays
{
    public class SingularityOverlay : Overlay, IConfigurable<TextureOverlayParameter>
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        public override bool RequestScreenTexture => true;
        private readonly ShaderInstance _shader;

        private Dictionary<string, KeyValuePair<string, string>> _nameToRSI;

        public SingularityOverlay() : base(nameof(SharedOverlayID.SingularityOverlay))
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
            if (_nameToRSI.TryGetValue("singularityTexture", out var singularityRSIPath){
                
                _shader?.SetParameter("singularityTexture", );
            }
            worldHandle.DrawRect(viewport, Color.White);
        }

        public void Configure(TextureOverlayParameter parameters)
        {
            _nameToRSI = parameters.ToDictionary();
        }
    }
}

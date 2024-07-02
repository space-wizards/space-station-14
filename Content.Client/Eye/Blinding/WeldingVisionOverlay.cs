using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Eye.Blinding.Components;


namespace Content.Client.Eye.Blinding
{
    public sealed class WeldingVisionOverlay : Overlay
    {
        [Dependency] private readonly IEntityManager _entitym = default!;
        [Dependency] private readonly IPlayerManager _playm = default!;
        [Dependency] private readonly IPrototypeManager _protom = default!;

        public override bool RequestScreenTexture => true;
        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        private readonly ShaderInstance _welderShader;

        private float _outDiam;
        private float _inDiam;

        public WeldingVisionOverlay()
        {
            IoCManager.InjectDependencies(this);
            _welderShader = _protom.Index<ShaderPrototype>("WeldingMaskOverlay").InstanceUnique();

            _welderShader.SetParameter("OuterDiameter", _outDiam);
            _welderShader.SetParameter("InnerDiameter", _inDiam);
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            if (!_entitym.TryGetComponent(_playm.LocalSession?.AttachedEntity, out EyeComponent? eyeComp))
                return false;
            if (args.Viewport.Eye != eyeComp.Eye)
                return false;

            var playent = _playm.LocalSession?.AttachedEntity;

            if (playent == null)
                return false;
            if (!_entitym.TryGetComponent<WeldingVisionComponent>(playent, out var weldComp))
                return false;

            if (_entitym.TryGetComponent<BlindableComponent>(playent, out var blindComp)
                && blindComp.IsBlind)
                return false;
            _inDiam = weldComp.InnerDiameter;
            _outDiam = weldComp.OuterDiameter;
            return true;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            var player = _playm.LocalSession?.AttachedEntity;

            var world = args.WorldHandle;
            var viewport = args.WorldBounds;

            float zoom = 1.0f;
            if (_entitym.TryGetComponent<EyeComponent>(player, out var eyeComp))
                zoom = eyeComp.Zoom.X;


            _welderShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _welderShader.SetParameter("Zoom", zoom);

            _welderShader.SetParameter("InDiameter", _inDiam);
            _welderShader.SetParameter("OutDiameter", _outDiam);

            world.UseShader(_welderShader);
            world.DrawRect(viewport, Color.White);
            world.UseShader(null);
        }

    }
}

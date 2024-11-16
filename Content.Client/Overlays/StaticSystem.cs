using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Overlays;
using Robust.Shared.Timing;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Overlays
{
    public sealed class StaticViewerHudSystem : EntitySystem
    {
        [Dependency] private readonly IOverlayManager _overlayMan = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        private ShaderInstance _staticViewerShader = null!;

        // Constructor: Ensure dependencies are injected
        public StaticViewerHudSystem()
        {
            IoCManager.InjectDependencies(this); // Ensure dependencies are injected
        }

        public override void Initialize()
        {
            base.Initialize();
            Log.Error($"Initialized static");

            // Load the shader prototype for "Grainy" effect
            _staticViewerShader = _prototypeManager.Index<ShaderPrototype>("Grainy").InstanceUnique();

            // Subscribe to equipment events to add/remove the overlay
            SubscribeLocalEvent<StaticViewerComponent, GotEquippedEvent>(OnCompEquip);
            SubscribeLocalEvent<StaticViewerComponent, GotUnequippedEvent>(OnCompUnequip);
        }

        private void OnCompEquip(EntityUid uid, StaticViewerComponent component, GotEquippedEvent args)
        {
            Log.Error($"equipped");

            // When the StaticViewer component is equipped, add the shader effect overlay
            var playerEntity = _playerManager.LocalEntity;
            if (playerEntity != null && _entityManager.HasComponent<EyeComponent>(playerEntity))
            {
                // Pass the necessary dependencies (entity manager, player manager) to the overlay
                _overlayMan.AddOverlay(new StaticViewerOverlay(_staticViewerShader, _entityManager, _playerManager));
            }
        }

        private void OnCompUnequip(EntityUid uid, StaticViewerComponent component, GotUnequippedEvent args)
        {
            Log.Error($"unequipped");

            // When the StaticViewer component is unequipped, remove the shader effect overlay
            _overlayMan.RemoveOverlay<StaticViewerOverlay>();
        }
    }

    // Custom overlay class to apply the shader effect
    public sealed class StaticViewerOverlay : Overlay
    {
        private readonly ShaderInstance _shaderInstance;
        private readonly IEntityManager _entityManager;
        private readonly IPlayerManager _playerManager;

        // Constructor now accepts dependencies
        public StaticViewerOverlay(ShaderInstance shaderInstance, IEntityManager entityManager, IPlayerManager playerManager)
        {
            _shaderInstance = shaderInstance;
            _entityManager = entityManager;
            _playerManager = playerManager;
        }

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        public override bool RequestScreenTexture => true;

 protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        return true;
    }




        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            var handle = args.WorldHandle;
            _shaderInstance.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            handle.UseShader(_shaderInstance);
            handle.DrawRect(args.WorldBounds, Color.White);
            handle.UseShader(null);
        }


    }
}

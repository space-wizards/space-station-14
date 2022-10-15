using Content.Client.Construction.UI;
using Content.Client.Hands;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Screens;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Client.Player;
using Robust.Client.GameObjects;

namespace Content.Client.Gameplay
{
    public sealed class GameplayState : GameplayStateBase, IMainViewportState
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _playerMan = default!;
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        protected override Type? LinkedScreenType => typeof(DefaultGameScreen);
        public static readonly Vector2i ViewportSize = (EyeManager.PixelsPerMeter * 21, EyeManager.PixelsPerMeter * 15);
        private FpsCounter _fpsCounter = default!;

        public MainViewport Viewport { get; private set; } = default!;

        public GameplayState()
        {
            IoCManager.InjectDependencies(this);
        }

        protected override void Startup()
        {
            base.Startup();
            Viewport = new MainViewport
            {
                Viewport =
                {
                    ViewportSize = ViewportSize
                }
            };
            UserInterfaceManager.StateRoot.AddChild(Viewport);
            LayoutContainer.SetAnchorPreset(Viewport, LayoutContainer.LayoutPreset.Wide);
            Viewport.SetPositionFirst();
            _eyeManager.MainViewport = Viewport.Viewport;
            _overlayManager.AddOverlay(new ShowHandItemOverlay());
            _fpsCounter = new FpsCounter(_gameTiming);
            UserInterfaceManager.PopupRoot.AddChild(_fpsCounter);
            _fpsCounter.Visible = _configurationManager.GetCVar(CCVars.HudFpsCounterVisible);
            _configurationManager.OnValueChanged(CCVars.HudFpsCounterVisible, (show) => { _fpsCounter.Visible = show; });
        }

        protected override void Shutdown()
        {
            _overlayManager.RemoveOverlay<ShowHandItemOverlay>();

            base.Shutdown();
            Viewport.Dispose();
            // Clear viewport to some fallback, whatever.
            _eyeManager.MainViewport = UserInterfaceManager.MainViewport;
            _fpsCounter.Dispose();
            _uiManager.ClearWindows();
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            base.FrameUpdate(e);

            Viewport.Viewport.Eye = _eyeManager.CurrentEye;

            // verify that the current eye is not "null". Fuck IEyeManager.

            var ent = _playerMan.LocalPlayer?.ControlledEntity;
            if (_eyeManager.CurrentEye.Position != default || ent == null)
                return;

            _entMan.TryGetComponent(ent, out EyeComponent? eye);
            
            if (eye?.Eye == _eyeManager.CurrentEye
                && _entMan.GetComponent<TransformComponent>(ent.Value).WorldPosition == default)
                return; // nothing to worry about, the player is just in null space... actually that is probably a problem?

            // Currently, this shouldn't happen. This likely happened because the main eye was set to null. When this
            // does happen it can create hard to troubleshoot bugs, so lets print some helpful warnings:
            Logger.Warning($"Main viewport's eye is in nullspace (main eye is null?). Attached entity: {_entMan.ToPrettyString(ent.Value)}. Entity has eye comp: {eye != null}");
        }

        protected override void OnKeyBindStateChanged(ViewportBoundKeyEventArgs args)
        {
            if (args.Viewport == null)
                base.OnKeyBindStateChanged(new ViewportBoundKeyEventArgs(args.KeyEventArgs, Viewport.Viewport));
            else
                base.OnKeyBindStateChanged(args);
        }
    }
}

using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Chat.UI
{
    public class TypingIndicatorGui : BoxContainer
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        public TypingIndicatorOverlay TalkOverlay { get; }
        public IEntity Entity { get; }

        public TypingIndicatorGui(IEntity entity)
        {
            IoCManager.InjectDependencies(this);
            IoCManager.Resolve<IUserInterfaceManager>().StateRoot.AddChild(this);
            SeparationOverride = 0;
            Orientation = LayoutOrientation.Vertical;

            TalkOverlay = new TypingIndicatorOverlay
            {
                VerticalAlignment = VAlignment.Center
            };

            AddChild(TalkOverlay);
            Entity = entity;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (Entity.Deleted ||
                _eyeManager.CurrentMap != Entity.Transform.MapID)
            {
                Visible = false;
                return;
            }

            Visible = true;

            var screenCoordinates = _eyeManager.CoordinatesToScreen(Entity.Transform.Coordinates);
            var playerPosition = UserInterfaceManager.ScreenToUIPosition(screenCoordinates);
            LayoutContainer.SetPosition(this, new Vector2(playerPosition.X - Width / 2, playerPosition.Y - Height - 30.0f));
        }
    }

    public class TypingIndicatorOverlay : Control
    {
        private ShaderInstance Shader { get; }
        private Texture TypingIndicatorTexture { get; }

        public TypingIndicatorOverlay()
        {
            IoCManager.InjectDependencies(this);
            Shader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("unshaded").Instance();
            var specifier = new SpriteSpecifier.Rsi(new ResourcePath("/Textures/Interface/Misc/talk.rsi"), "h0");
            TypingIndicatorTexture = specifier.Frame0();
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);
            handle.UseShader(Shader);
            handle.DrawTexture(TypingIndicatorTexture, Vector2.Zero);
        }
    }
}

#nullable enable
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.GameObjects.EntitySystems.HealthOverlay
{
    public class HealthOverlayGui : VBoxContainer
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        public HealthOverlayGui(IEntity entity)
        {
            IoCManager.InjectDependencies(this);
            IoCManager.Resolve<IUserInterfaceManager>().StateRoot.AddChild(this);
            SeparationOverride = 0;

            CritBar = new HealthOverlayBar
            {
                Visible = false,
                VerticalAlignment = VAlignment.Center,
                Color = Color.Red
            };

            HealthBar = new HealthOverlayBar
            {
                Visible = false,
                VerticalAlignment = VAlignment.Center,
                Color = Color.Green
            };

            AddChild(Panel = new PanelContainer
            {
                Children =
                {
                    new TextureRect
                    {
                        Texture = StaticIoC.ResC.GetTexture("/Textures/Interface/Misc/health_bar.rsi/icon.png"),
                        TextureScale = Vector2.One * HealthOverlayBar.HealthBarScale,
                        VerticalAlignment = VAlignment.Center,
                    },
                    CritBar,
                    HealthBar
                }
            });

            Entity = entity;
        }

        public PanelContainer Panel { get; }

        public HealthOverlayBar HealthBar { get; }

        public HealthOverlayBar CritBar { get; }

        public IEntity Entity { get; }

        public void SetVisibility(bool val)
        {
            Visible = val;
            Panel.Visible = val;
        }

        private void MoreFrameUpdate(FrameEventArgs args)
        {
            if (Entity.Deleted)
            {
                return;
            }

            if (!Entity.TryGetComponent(out IMobStateComponent? mobState) ||
                !Entity.TryGetComponent(out IDamageableComponent? damageable))
            {
                CritBar.Visible = false;
                HealthBar.Visible = false;
                return;
            }

            int threshold;

            if (mobState.IsAlive())
            {
                if (!mobState.TryGetEarliestCriticalState(damageable.TotalDamage, out _, out threshold))
                {
                    CritBar.Visible = false;
                    HealthBar.Visible = false;
                    return;
                }

                CritBar.Ratio = 1;
                CritBar.Visible = true;
                HealthBar.Ratio = 1 - (float) damageable.TotalDamage / threshold;
                HealthBar.Visible = true;
            }
            else if (mobState.IsCritical())
            {
                HealthBar.Ratio = 0;
                HealthBar.Visible = false;

                if (!mobState.TryGetPreviousCriticalState(damageable.TotalDamage, out _, out var critThreshold) ||
                    !mobState.TryGetEarliestDeadState(damageable.TotalDamage, out _, out var deadThreshold))
                {
                    CritBar.Visible = false;
                    return;
                }

                CritBar.Visible = true;
                CritBar.Ratio = 1 - (float)
                    (damageable.TotalDamage - critThreshold) /
                    (deadThreshold - critThreshold);
            }
            else if (mobState.IsDead())
            {
                CritBar.Ratio = 0;
                CritBar.Visible = false;
                HealthBar.Ratio = 0;
                HealthBar.Visible = true;
            }
            else
            {
                CritBar.Visible = false;
                HealthBar.Visible = false;
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            MoreFrameUpdate(args);

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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            HealthBar.Dispose();
        }
    }
}

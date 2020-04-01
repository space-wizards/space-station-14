using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.AI;
using Content.Shared.Pathfinding;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.GameObjects.Components.AI
{
    /// <summary>
    /// This handles everything related to tooltips above AI mobs
    /// </summary>
    [RegisterComponent]
    public sealed class MobTooltipDebugComponent : SharedAiDebugComponent
    {
        private static int _tooltips;

        public static void DisableAll()
        {
            _tooltips = 0;
            Toggle?.Invoke();
        }

        public static void EnableTooltip(MobTooltips tooltip)
        {
            _tooltips |= (int) tooltip;
            Toggle?.Invoke();
        }

        public static void DisableTooltip(MobTooltips tooltip)
        {
            _tooltips &= ~(int) tooltip;
            Toggle?.Invoke();
        }

        public static void ToggleTooltip(MobTooltips tooltip)
        {
            if ((_tooltips & (int) tooltip) != 0)
            {
                DisableTooltip(tooltip);
            }
            else
            {
                EnableTooltip(tooltip);
            }
        }

        private static event Action Toggle;

        // Ideally you'd persist the label until the entity dies / is deleted but for first-draft this is fine
        private DebugAiOverlay _overlay;

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);
            switch (message)
            {
                case UtilityAiDebugMessage msg:
                    if ((_tooltips & (int) MobTooltips.Thonk) != 0)
                    {
                        _overlay?.UpdatePlan(msg);
                    }
                    break;
                case AStarRouteMessage route:
                    if ((_tooltips & (int) MobTooltips.Paths) != 0)
                    {
                        _overlay?.UpdateAStarRoute(route);
                    }
                    break;
                case JpsRouteMessage route:
                    if ((_tooltips & (int) MobTooltips.Paths) != 0)
                    {
                        _overlay?.UpdateJpsRoute(route);
                    }
                    break;
            }
        }

        private void ToggleOverlay()
        {
            if (_tooltips != 0)
            {
                if (_overlay != null)
                {
                    return;
                }
                var overlayManager = IoCManager.Resolve<IOverlayManager>();
                _overlay = new DebugAiOverlay();
                overlayManager.AddOverlay(_overlay);
                return;
            }
            else
            {
                if (_overlay == null)
                {
                    return;
                }

                var overlayManager = IoCManager.Resolve<IOverlayManager>();
                overlayManager.RemoveOverlay(_overlay.ID);
                _overlay = null;
            }
        }

        public override void OnAdd()
        {
            base.OnAdd();
            Toggle += ToggleOverlay;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (_overlay != null)
            {
                var overlayManager = IoCManager.Resolve<IOverlayManager>();
                overlayManager.RemoveOverlay(_overlay.ID);
                _overlay = null;
            }
            Toggle -= ToggleOverlay;
        }
    }

    public sealed class DebugAiOverlay : Overlay
    {
        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        private Dictionary<IEntity, PanelContainer> _aiBoxes = new Dictionary<IEntity, PanelContainer>();

        public DebugAiOverlay() : base(nameof(DebugAiOverlay))
        {
            Shader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("unshaded").Instance();
        }

        private bool TryCreatePanel(IEntity entity)
        {
            if (!_aiBoxes.ContainsKey(entity))
            {
                var userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();

                var actionLabel = new Label
                {
                    MouseFilter = Control.MouseFilterMode.Ignore,
                };

                var pathfindingLabel = new Label
                {
                    MouseFilter = Control.MouseFilterMode.Ignore,
                };

                var vBox = new VBoxContainer()
                {
                    SeparationOverride = 15,
                    Children = {actionLabel, pathfindingLabel},
                };

                var panel = new PanelContainer
                {
                    StyleClasses = {"tooltipBox"},
                    Children = {vBox},
                    MouseFilter = Control.MouseFilterMode.Ignore,
                    ModulateSelfOverride = Color.White.WithAlpha(0.75f),
                };


                userInterfaceManager.StateRoot.AddChild(panel);

                _aiBoxes[entity] = panel;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updated when we receive an AI pathfinding route
        /// </summary>
        /// <param name="message"></param>
        public void UpdateJpsRoute(JpsRouteMessage message)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var entity = entityManager.GetEntity(message.EntityUid);
            if (entity == null)
            {
                return;
            }

            TryCreatePanel(entity);

            var label = (Label) _aiBoxes[entity].GetChild(0).GetChild(1);
            label.Text = $"Pathfinding time (ms): {message.TimeTaken * 1000:0.0000}\n" +
                         $"Jump Nodes: {message.JumpNodes.Count}\n" +
                         $"Jump Nodes per ms: {message.JumpNodes.Count / (message.TimeTaken * 1000)}";
        }

        /// <summary>
        /// Updated when we receive an AI pathfinding route
        /// </summary>
        /// <param name="message"></param>
        public void UpdateAStarRoute(AStarRouteMessage message)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var entity = entityManager.GetEntity(message.EntityUid);
            if (entity == null)
            {
                return;
            }

            TryCreatePanel(entity);

            var label = (Label) _aiBoxes[entity].GetChild(0).GetChild(1);
            label.Text = $"Pathfinding time (ms): {message.TimeTaken * 1000:0.0000}\n" +
                         $"Nodes traversed: {message.ClosedTiles.Count}\n" +
                         $"Nodes per ms: {message.ClosedTiles.Count / (message.TimeTaken * 1000)}";
        }

        /// <summary>
        /// Updated when we receive the AI's action
        /// </summary>
        /// <param name="message"></param>
        public void UpdatePlan(UtilityAiDebugMessage message)
        {
            // I guess if it's out of range we don't know about it?
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var entity = entityManager.GetEntity(message.EntityUid);
            if (entity == null)
            {
                return;
            }

            TryCreatePanel(entity);

            // Probably shouldn't access by index but it's a debugging tool so eh
            var label = (Label) _aiBoxes[entity].GetChild(0).GetChild(0);
            label.Text = $"Current Task: {message.FoundTask}\n" +
                         $"Task score: {message.ActionScore}\n" +
                         $"Planning time (ms): {message.PlanningTime * 1000:0.0000}\n" +
                         $"Considered {message.ConsideredTaskCount} tasks";
        }

        protected override void Draw(DrawingHandleBase handle)
        {
            var eyeManager = IoCManager.Resolve<IEyeManager>();
            foreach (var (entity, panel) in _aiBoxes)
            {
                if (entity == null) continue;

                if (!eyeManager.GetWorldViewport().Contains(entity.Transform.WorldPosition))
                {
                    panel.Visible = false;
                    continue;
                }

                var screenPosition = eyeManager.WorldToScreen(entity.Transform.GridPosition).Position;
                var offsetPosition = new Vector2(screenPosition.X - panel.Width / 2, screenPosition.Y - panel.Height - 50f);
                panel.Visible = true;

                LayoutContainer.SetPosition(panel, offsetPosition);
            }
        }
    }

    [Flags]
    public enum MobTooltips : byte
    {
        None = 0,
        Paths = 1 << 1,
        Thonk = 1 << 2,
    }
}

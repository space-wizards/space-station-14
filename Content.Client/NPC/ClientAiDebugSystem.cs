using Content.Client.Stylesheets;
using Content.Shared.AI;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.NPC
{
    public sealed class ClientAiDebugSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        public AiDebugMode Tooltips { get; private set; } = AiDebugMode.None;
        private readonly Dictionary<EntityUid, PanelContainer> _aiBoxes = new();

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (Tooltips == 0)
            {
                if (_aiBoxes.Count > 0)
                {
                    foreach (var (_, panel) in _aiBoxes)
                    {
                        panel.Dispose();
                    }

                    _aiBoxes.Clear();
                }
                return;
            }

            var deletedEntities = new List<EntityUid>(0);
            foreach (var (entity, panel) in _aiBoxes)
            {
                if (Deleted(entity))
                {
                    deletedEntities.Add(entity);
                    continue;
                }

                if (!_eyeManager.GetWorldViewport().Contains(EntityManager.GetComponent<TransformComponent>(entity).WorldPosition))
                {
                    panel.Visible = false;
                    continue;
                }

                var (x, y) = _eyeManager.CoordinatesToScreen(EntityManager.GetComponent<TransformComponent>(entity).Coordinates).Position;
                var offsetPosition = new Vector2(x - panel.Width / 2, y - panel.Height - 50f);
                panel.Visible = true;

                LayoutContainer.SetPosition(panel, offsetPosition);
            }

            foreach (var entity in deletedEntities)
            {
                _aiBoxes.Remove(entity);
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            UpdatesOutsidePrediction = true;
            SubscribeNetworkEvent<SharedAiDebug.UtilityAiDebugMessage>(HandleUtilityAiDebugMessage);
            SubscribeNetworkEvent<SharedAiDebug.AStarRouteMessage>(HandleAStarRouteMessage);
            SubscribeNetworkEvent<SharedAiDebug.JpsRouteMessage>(HandleJpsRouteMessage);
        }

        private void HandleUtilityAiDebugMessage(SharedAiDebug.UtilityAiDebugMessage message)
        {
            if ((Tooltips & AiDebugMode.Thonk) != 0)
            {
                // I guess if it's out of range we don't know about it?
                var entity = message.EntityUid;
                TryCreatePanel(entity);

                // Probably shouldn't access by index but it's a debugging tool so eh
                var label = (Label) _aiBoxes[entity].GetChild(0).GetChild(0);
                label.Text = $"Current Task: {message.FoundTask}\n" +
                             $"Task score: {message.ActionScore}\n" +
                             $"Planning time (ms): {message.PlanningTime * 1000:0.0000}\n" +
                             $"Considered {message.ConsideredTaskCount} tasks";
            }
        }

        private void HandleAStarRouteMessage(SharedAiDebug.AStarRouteMessage message)
        {
            if ((Tooltips & AiDebugMode.Paths) != 0)
            {
                var entity = message.EntityUid;
                TryCreatePanel(entity);

                var label = (Label) _aiBoxes[entity].GetChild(0).GetChild(1);
                label.Text = $"Pathfinding time (ms): {message.TimeTaken * 1000:0.0000}\n" +
                             $"Nodes traversed: {message.CameFrom.Count}\n" +
                             $"Nodes per ms: {message.CameFrom.Count / (message.TimeTaken * 1000)}";
            }
        }

        private void HandleJpsRouteMessage(SharedAiDebug.JpsRouteMessage message)
        {
            if ((Tooltips & AiDebugMode.Paths) != 0)
            {
                var entity = message.EntityUid;
                TryCreatePanel(entity);

                var label = (Label) _aiBoxes[entity].GetChild(0).GetChild(1);
                label.Text = $"Pathfinding time (ms): {message.TimeTaken * 1000:0.0000}\n" +
                             $"Jump Nodes: {message.JumpNodes.Count}\n" +
                             $"Jump Nodes per ms: {message.JumpNodes.Count / (message.TimeTaken * 1000)}";
            }
        }

        public void Disable()
        {
            foreach (var tooltip in _aiBoxes.Values)
            {
                tooltip.Dispose();
            }
            _aiBoxes.Clear();
            Tooltips = AiDebugMode.None;
        }


        public void EnableTooltip(AiDebugMode tooltip)
        {
            Tooltips |= tooltip;
        }

        public void DisableTooltip(AiDebugMode tooltip)
        {
            Tooltips &= ~tooltip;
        }

        public void ToggleTooltip(AiDebugMode tooltip)
        {
            if ((Tooltips & tooltip) != 0)
            {
                DisableTooltip(tooltip);
            }
            else
            {
                EnableTooltip(tooltip);
            }
        }

        private bool TryCreatePanel(EntityUid entity)
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

                var vBox = new BoxContainer()
                {
                    Orientation = LayoutOrientation.Vertical,
                    SeparationOverride = 15,
                    Children = {actionLabel, pathfindingLabel},
                };

                var panel = new PanelContainer
                {
                    StyleClasses = { StyleNano.StyleClassTooltipPanel },
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
    }

    [Flags]
    public enum AiDebugMode : byte
    {
        None = 0,
        Paths = 1 << 1,
        Thonk = 1 << 2,
    }
}

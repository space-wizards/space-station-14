using System.Threading;
using System.Threading.Tasks;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ExamineSystem : ExamineSystemShared
    {
        public const string StyleClassEntityTooltip = "entity-tooltip";

#pragma warning disable 649
        [Dependency] private IInputManager _inputManager;
        [Dependency] private IUserInterfaceManager _userInterfaceManager;
        [Dependency] private IEntityManager _entityManager;
        [Dependency] private IPlayerManager _playerManager;
#pragma warning restore 649

        private Popup _examineTooltipOpen;
        private CancellationTokenSource _requestCancelTokenSource;

        public override void Initialize()
        {
            IoCManager.InjectDependencies(this);

            var inputSys = EntitySystemManager.GetEntitySystem<InputSystem>();
            inputSys.BindMap.BindFunction(ContentKeyFunctions.ExamineEntity, new PointerInputCmdHandler(HandleExamine));
        }

        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();

            RegisterMessageType<ExamineSystemMessages.ExamineInfoResponseMessage>();
        }

        private void HandleExamine(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            if (!uid.IsValid() || !_entityManager.TryGetEntity(uid, out var examined))
            {
                return;
            }

            var playerEntity = _playerManager.LocalPlayer.ControlledEntity;

            if (playerEntity == null || !CanExamine(playerEntity, examined))
            {
                return;
            }

            DoExamine(examined);
        }

        public async void DoExamine(IEntity entity)
        {
            CloseTooltip();

            var mousePos = _inputManager.MouseScreenPosition;

            // Actually open the tooltip.
            _examineTooltipOpen = new Popup();
            _userInterfaceManager.StateRoot.AddChild(_examineTooltipOpen);
            var panel = new PanelContainer();
            panel.AddStyleClass(StyleClassEntityTooltip);
            panel.ModulateSelfOverride = Color.LightGray.WithAlpha(0.90f);
            _examineTooltipOpen.AddChild(panel);
            panel.SetAnchorAndMarginPreset(Control.LayoutPreset.Wide);
            var vBox = new VBoxContainer();
            panel.AddChild(vBox);
            var hBox = new HBoxContainer { SeparationOverride = 5};
            vBox.AddChild(hBox);
            if (entity.TryGetComponent(out ISpriteComponent sprite))
            {
                hBox.AddChild(new SpriteView {Sprite = sprite});
            }

            hBox.AddChild(new Label
            {
                Text = entity.Name,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
            });

            const float minWidth = 300;
            var size = Vector2.ComponentMax((minWidth, 0), panel.CombinedMinimumSize);
            _examineTooltipOpen.Open(UIBox2.FromDimensions(mousePos, size));

            if (entity.Uid.IsClientSide())
            {
                return;
            }

            // Ask server for extra examine info.
            RaiseNetworkEvent(new ExamineSystemMessages.RequestExamineInfoMessage(entity.Uid));

            ExamineSystemMessages.ExamineInfoResponseMessage response;
            try
            {
                _requestCancelTokenSource = new CancellationTokenSource();
                response =
                    await AwaitNetMessage<ExamineSystemMessages.ExamineInfoResponseMessage>(_requestCancelTokenSource
                        .Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            finally
            {
                _requestCancelTokenSource = null;
            }

            var richLabel = new RichTextLabel();
            richLabel.SetMessage(response.Message);
            vBox.AddChild(richLabel);
        }

        public void CloseTooltip()
        {
            if (_examineTooltipOpen != null)
            {
                _examineTooltipOpen.Dispose();
                _examineTooltipOpen = null;
            }

            if (_requestCancelTokenSource != null)
            {
                _requestCancelTokenSource.Cancel();
                _requestCancelTokenSource = null;
            }
        }
    }
}

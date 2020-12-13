using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Utility;
using Content.Shared.GameObjects.Components.Inventory;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ExamineSystem : ExamineSystemShared
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        public const string StyleClassEntityTooltip = "entity-tooltip";

        private Popup _examineTooltipOpen;
        private CancellationTokenSource _requestCancelTokenSource;
        private IEntity _examinedEntity = null;

        public override void Initialize()
        {
            IoCManager.InjectDependencies(this);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.ExamineEntity, new PointerInputCmdHandler(HandleExamine))
                .Register<ExamineSystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<ExamineSystem>();
            base.Shutdown();
        }

        private bool HandleExamine(ICommonSession session, EntityCoordinates coords, EntityUid uid)
        {
            if (!uid.IsValid() || !EntityManager.TryGetEntity(uid, out var examined))
            {
                return false;
            }

            var playerEntity = _playerManager.LocalPlayer.ControlledEntity;

            if (playerEntity == null || !CanExamine(playerEntity, examined))
            {
                return false;
            }

            DoExamine(examined);
            return true;
        }

        public override void Update(float frameTime)
        {
            // Can we still examine the entity?
            // If not then remove the target entity from the popup to stop it drawing a line
            if (_examinedEntity != null && _examineTooltipOpen != null)
            {
                var playerEntity = _playerManager.LocalPlayer.ControlledEntity;

                if (playerEntity == null || _examinedEntity == null || !CanExamine(playerEntity, _examinedEntity))
                {
                    PopupContainer.SetTargetEntityProperty(_examineTooltipOpen, null);
                }
            }


            base.Update(frameTime);
        }

        public async void DoExamine(IEntity entity)
        {
            const float minWidth = 300;
            CloseTooltip();
            _examinedEntity = entity;

            var popupPos = _userInterfaceManager.MousePositionScaled;

            // Actually open the tooltip.
            _examineTooltipOpen = new Popup();
            PopupContainer.SetTargetEntityProperty(_examineTooltipOpen, _examinedEntity);
            _userInterfaceManager.ModalRoot.AddChild(_examineTooltipOpen);
            var panel = new PanelContainer();
            panel.AddStyleClass(StyleClassEntityTooltip);
            panel.ModulateSelfOverride = Color.LightGray.WithAlpha(0.90f);
            _examineTooltipOpen.AddChild(panel);
            //panel.SetAnchorAndMarginPreset(Control.LayoutPreset.Wide);
            var vBox = new VBoxContainer();
            panel.AddChild(vBox);
            var hBox = new HBoxContainer {SeparationOverride = 5};
            vBox.AddChild(hBox);
            if (_examinedEntity.TryGetComponent(out ISpriteComponent sprite))
            {
                hBox.AddChild(new SpriteView {Sprite = sprite});
            }

            hBox.AddChild(new Label
            {
                Text = _examinedEntity.Name,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
            });

            var size = Vector2.ComponentMax((minWidth, 0), panel.CombinedMinimumSize);

            _examineTooltipOpen.Open(UIBox2.FromDimensions(popupPos, size));

            FormattedMessage message;
            if (_examinedEntity.Uid.IsClientSide())
            {
                message = GetExamineText(_examinedEntity, _playerManager.LocalPlayer.ControlledEntity);
            }
            else
            {

                // Ask server for extra examine info.
                RaiseNetworkEvent(new ExamineSystemMessages.RequestExamineInfoMessage(_examinedEntity.Uid));

                ExamineSystemMessages.ExamineInfoResponseMessage response;
                try
                {
                    _requestCancelTokenSource = new CancellationTokenSource();
                    response =
                        await AwaitNetworkEvent<ExamineSystemMessages.ExamineInfoResponseMessage>(_requestCancelTokenSource
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

                message = response.Message;
            }

            foreach (var msg in message.Tags.OfType<FormattedMessage.TagText>())
            {
                if (!string.IsNullOrWhiteSpace(msg.Text))
                {
                    var richLabel = new RichTextLabel();
                    richLabel.SetMessage(message);
                    vBox.AddChild(richLabel);
                    break;
                }
            }
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

            _examinedEntity = null;
        }
    }
}

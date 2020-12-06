using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Input;
using JetBrains.Annotations;
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
        private Vector2 _examineOffset = default;
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
            base.Update(frameTime);

            if (_examinedEntity == null | _playerManager.LocalPlayer.ControlledEntity == null)
            {
                return;
            }

            // Can we even still see the item we examined?
            if(!CanExamine(_playerManager.LocalPlayer.ControlledEntity, _examinedEntity))
            {
                CloseTooltip();
                return;
            }

            var drawPos = _eyeManager.WorldToScreen(_examinedEntity.Transform.MapPosition.Position) + _examineOffset;

            // If the draw point is outside the screen then just close the popup
            // TODO

            PopupContainer.SetPopupOrigin(_examineTooltipOpen, drawPos);
        }

        public async void DoExamine(IEntity entity)
        {
            if (entity == null) { return; }

            const float minWidth = 300;
            var popupPos = _userInterfaceManager.MousePositionScaled;

            CloseTooltip();

            _examinedEntity = entity;

            // Find where we clicked on the entity specifically
            _examineOffset = new Vector2(_userInterfaceManager.MousePositionScaled.X - _eyeManager.WorldToScreen(entity.Transform.MapPosition.Position).X,
                                         _userInterfaceManager.MousePositionScaled.Y - _eyeManager.WorldToScreen(entity.Transform.MapPosition.Position).Y);

            // Actually open the tooltip.
            _examineTooltipOpen = new Popup();
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

            var size = Vector2.ComponentMax((minWidth, 0), panel.CombinedMinimumSize);
            size = Vector2.ComponentMax(size, _examineTooltipOpen.CombinedPixelMinimumSize);

            // If the target entity is so close to the edge of the screen
            // that the popup would open partially offscreen
            // then move the offset back inside the screen
            if (popupPos.X + size.X > _userInterfaceManager.WindowRoot.Width)
            {
                var xOffset = (popupPos.X + size.X) - _userInterfaceManager.WindowRoot.Width;
                popupPos.X -= xOffset;
                _examineOffset.X -= xOffset;
            }

            if (popupPos.Y + size.Y > _userInterfaceManager.WindowRoot.Height)
            {
                var yOffset = (popupPos.Y + size.Y) - _userInterfaceManager.WindowRoot.Height;
                popupPos.Y -= yOffset;
                _examineOffset.Y -= yOffset;
            }

            _examineTooltipOpen.Open(UIBox2.FromDimensions(popupPos, size), null, true);
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
            _examineOffset = default;
        }
    }
}

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
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

        public const string StyleClassEntityTooltip = "entity-tooltip";

        private Popup? _examineTooltipOpen;
        private CancellationTokenSource? _requestCancelTokenSource;

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

        private bool HandleExamine(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (!uid.IsValid() || !EntityManager.TryGetEntity(uid, out var examined))
            {
                return false;
            }

            var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;

            if (playerEntity == null || !CanExamine(playerEntity, examined))
            {
                return false;
            }

            DoExamine(examined);
            return true;
        }

        public async void DoExamine(IEntity entity)
        {
            const float minWidth = 300;
            CloseTooltip();

            var popupPos = _userInterfaceManager.MousePositionScaled;

            // Actually open the tooltip.
            _examineTooltipOpen = new Popup { MaxWidth = 400};
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
            if (entity.TryGetComponent(out ISpriteComponent? sprite))
            {
                hBox.AddChild(new SpriteView {Sprite = sprite});
            }

            hBox.AddChild(new Label
            {
                Text = entity.Name,
                HorizontalExpand = true,
            });

            panel.Measure(Vector2.Infinity);
            var size = Vector2.ComponentMax((minWidth, 0), panel.DesiredSize);

            _examineTooltipOpen.Open(UIBox2.FromDimensions(popupPos.Position, size));

            FormattedMessage message;
            if (entity.Uid.IsClientSide())
            {
                message = GetExamineText(entity, _playerManager.LocalPlayer?.ControlledEntity);
            }
            else
            {

                // Ask server for extra examine info.
                RaiseNetworkEvent(new ExamineSystemMessages.RequestExamineInfoMessage(entity.Uid));

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
        }
    }
}

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Examine;
using Content.Shared.Input;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Utility;
using static Content.Shared.Interaction.SharedInteractionSystem;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Examine
{
    [UsedImplicitly]
    internal sealed class ExamineSystem : ExamineSystemShared
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public const string StyleClassEntityTooltip = "entity-tooltip";

        private IEntity? _examinedEntity;
        private IEntity? _playerEntity;
        private Popup? _examineTooltipOpen;
        private CancellationTokenSource? _requestCancelTokenSource;

        public override void Initialize()
        {
            IoCManager.InjectDependencies(this);

            SubscribeLocalEvent<GetOtherVerbsEvent>(AddExamineVerb);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.ExamineEntity, new PointerInputCmdHandler(HandleExamine))
                .Register<ExamineSystem>();
        }

        public override void Update(float frameTime)
        {
            if (_examineTooltipOpen == null || !_examineTooltipOpen.Visible) return;
            if (_examinedEntity == null || _playerEntity == null) return;

            Ignored predicate = entity => entity == _playerEntity || entity == _examinedEntity;

            if (_playerEntity.TryGetContainer(out var container))
            {
                predicate += entity => entity == container.Owner;
            }

            if (!InRangeUnOccluded(_playerEntity, _examinedEntity, ExamineRange, predicate))
            {
                CloseTooltip();
            }
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<ExamineSystem>();
            base.Shutdown();
        }

        private bool HandleExamine(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (!uid.IsValid() || !EntityManager.TryGetEntity(uid, out var entity))
            {
                return false;
            }

            _playerEntity = _playerManager.LocalPlayer?.ControlledEntity;

            if (_playerEntity == null || !CanExamine(_playerEntity, entity))
            {
                return false;
            }

            DoExamine(entity);
            return true;
        }

        private void AddExamineVerb(GetOtherVerbsEvent args)
        {
            if (!CanExamine(args.User, args.Target))
                return;

            Verb verb = new();
            verb.Act = () => DoExamine(args.Target) ;
            verb.Text = Loc.GetString("examine-verb-name");
            verb.IconTexture = "/Textures/Interface/VerbIcons/examine.svg.192dpi.png";
            verb.ClientExclusive = true;
            args.Verbs.Add(verb);
        }

        public async void DoExamine(IEntity entity)
        {
            // Close any examine tooltip that might already be opened
            CloseTooltip();

            // cache entity for Update function
            _examinedEntity = entity;

            const float minWidth = 300;
            var popupPos = _userInterfaceManager.MousePositionScaled;

            // Actually open the tooltip.
            _examineTooltipOpen = new Popup { MaxWidth = 400 };
            _userInterfaceManager.ModalRoot.AddChild(_examineTooltipOpen);
            var panel = new PanelContainer();
            panel.AddStyleClass(StyleClassEntityTooltip);
            panel.ModulateSelfOverride = Color.LightGray.WithAlpha(0.90f);
            _examineTooltipOpen.AddChild(panel);

            var vBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            panel.AddChild(vBox);

            var hBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 5
            };
            vBox.AddChild(hBox);

            if (entity.TryGetComponent(out ISpriteComponent? sprite))
            {
                hBox.AddChild(new SpriteView { Sprite = sprite, OverrideDirection = Direction.South });
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
                if (string.IsNullOrWhiteSpace(msg.Text)) continue;

                var richLabel = new RichTextLabel();
                richLabel.SetMessage(message);
                vBox.AddChild(richLabel);
                break;
            }
        }

        private void CloseTooltip()
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

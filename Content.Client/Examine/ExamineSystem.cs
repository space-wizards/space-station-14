using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Examine;
using Content.Shared.Input;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
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
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        public const string StyleClassEntityTooltip = "entity-tooltip";

        private EntityUid _examinedEntity;
        private EntityUid _playerEntity;
        private Popup? _examineTooltipOpen;
        private CancellationTokenSource? _requestCancelTokenSource;

        public override void Initialize()
        {
            UpdatesOutsidePrediction = true;

            SubscribeLocalEvent<GetOtherVerbsEvent>(AddExamineVerb);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.ExamineEntity, new PointerInputCmdHandler(HandleExamine, outsidePrediction: true))
                .Register<ExamineSystem>();
        }

        public override void Update(float frameTime)
        {
            if (_examineTooltipOpen is not {Visible: true}) return;
            if (!_examinedEntity.Valid || !_playerEntity.Valid) return;

            if (!CanExamine(_playerEntity, _examinedEntity))
                CloseTooltip();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<ExamineSystem>();
            base.Shutdown();
        }

        public override bool CanExamine(EntityUid examiner, MapCoordinates target, Ignored? predicate = null)
        {
            var b = _eyeManager.GetWorldViewbounds();
            if (!b.Contains(target.Position))
                return false;

            return base.CanExamine(examiner, target, predicate);
        }

        private bool HandleExamine(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (!args.EntityUid.IsValid() || !EntityManager.EntityExists(args.EntityUid))
            {
                return false;
            }

            _playerEntity = _playerManager.LocalPlayer?.ControlledEntity ?? default;

            if (_playerEntity == default || !CanExamine(_playerEntity, args.EntityUid))
            {
                return false;
            }

            DoExamine(args.EntityUid);
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

        public async void DoExamine(EntityUid entity)
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

            if (EntityManager.TryGetComponent(entity, out ISpriteComponent? sprite))
            {
                hBox.AddChild(new SpriteView { Sprite = sprite, OverrideDirection = Direction.South });
            }

            hBox.AddChild(new Label
            {
                Text = EntityManager.GetComponent<MetaDataComponent>(entity).EntityName,
                HorizontalExpand = true,
            });

            panel.Measure(Vector2.Infinity);
            var size = Vector2.ComponentMax((minWidth, 0), panel.DesiredSize);

            _examineTooltipOpen.Open(UIBox2.FromDimensions(popupPos.Position, size));

            FormattedMessage message;
            if (entity.IsClientSide())
            {
                message = GetExamineText(entity, _playerManager.LocalPlayer?.ControlledEntity);
            }
            else
            {
                // Ask server for extra examine info.
                RaiseNetworkEvent(new ExamineSystemMessages.RequestExamineInfoMessage(entity));

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

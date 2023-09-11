using Content.Client.Verbs;
using Content.Shared.Eye.Blinding;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;
using System.Threading;
using Content.Shared.Eye.Blinding.Components;
using Robust.Client;
using static Content.Shared.Interaction.SharedInteractionSystem;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client.Examine
{
    [UsedImplicitly]
    public sealed class ExamineSystem : ExamineSystemShared
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly VerbSystem _verbSystem = default!;
        [Dependency] private readonly IBaseClient _client = default!;

        public const string StyleClassEntityTooltip = "entity-tooltip";

        private EntityUid _examinedEntity;
        private EntityUid _lastExaminedEntity;
        private EntityUid _playerEntity;
        private Popup? _examineTooltipOpen;
        private ScreenCoordinates _popupPos;
        private CancellationTokenSource? _requestCancelTokenSource;
        private int _idCounter;

        public override void Initialize()
        {
            UpdatesOutsidePrediction = true;

            SubscribeLocalEvent<GetVerbsEvent<ExamineVerb>>(AddExamineVerb);

            SubscribeNetworkEvent<ExamineSystemMessages.ExamineInfoResponseMessage>(OnExamineInfoResponse);

            SubscribeLocalEvent<ItemComponent, DroppedEvent>(OnExaminedItemDropped);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.ExamineEntity, new PointerInputCmdHandler(HandleExamine, outsidePrediction: true))
                .Register<ExamineSystem>();

            _idCounter = 0;
        }

        private void OnExaminedItemDropped(EntityUid item, ItemComponent comp, DroppedEvent args)
        {
            if (!args.User.Valid)
                return;
            if (_playerManager.LocalPlayer == null)
                return;
            if (_examineTooltipOpen == null)
                return;

            if (item == _examinedEntity && args.User == _playerManager.LocalPlayer.ControlledEntity)
                CloseTooltip();
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

        public override bool CanExamine(EntityUid examiner, MapCoordinates target, Ignored? predicate = null, EntityUid? examined = null, ExaminerComponent? examinerComp = null)
        {
            if (!Resolve(examiner, ref examinerComp, false))
                return false;

            if (examinerComp.SkipChecks)
                return true;

            if (examinerComp.CheckInRangeUnOccluded)
            {
                // TODO fix this. This should be using the examiner's eye component, not eye manager.
                var b = _eyeManager.GetWorldViewbounds();
                if (!b.Contains(target.Position))
                    return false;
            }

            return base.CanExamine(examiner, target, predicate, examined, examinerComp);
        }

        private bool HandleExamine(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            var entity = args.EntityUid;

            if (!args.EntityUid.IsValid() || !EntityManager.EntityExists(entity))
            {
                return false;
            }

            _playerEntity = _playerManager.LocalPlayer?.ControlledEntity ?? default;

            if (_playerEntity == default || !CanExamine(_playerEntity, entity))
            {
                return false;
            }

            DoExamine(entity);
            return true;
        }

        private void AddExamineVerb(GetVerbsEvent<ExamineVerb> args)
        {
            if (!CanExamine(args.User, args.Target))
                return;

            // Basic examine verb.
            ExamineVerb verb = new();
            verb.Category = VerbCategory.Examine;
            verb.Priority = 10;
            // Center it on the entity if they use the verb instead.
            verb.Act = () => DoExamine(args.Target, false);
            verb.Text = Loc.GetString("examine-verb-name");
            verb.Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/examine.svg.192dpi.png"));
            verb.ShowOnExamineTooltip = false;
            verb.ClientExclusive = true;
            args.Verbs.Add(verb);
        }

        private void OnExamineInfoResponse(ExamineSystemMessages.ExamineInfoResponseMessage ev)
        {
            var player = _playerManager.LocalPlayer?.ControlledEntity;
            if (player == null)
                return;

            // Prevent updating a new tooltip.
            if (ev.Id != 0 && ev.Id != _idCounter)
                return;

            // Tooltips coming in from the server generally prioritize
            // opening at the old tooltip rather than the cursor/another entity,
            // since there's probably one open already if it's coming in from the server.
            var entity = GetEntity(ev.EntityUid);

            OpenTooltip(player.Value, entity, ev.CenterAtCursor, ev.OpenAtOldTooltip, ev.KnowTarget);
            UpdateTooltipInfo(player.Value, entity, ev.Message, ev.Verbs);
        }

        public override void SendExamineTooltip(EntityUid player, EntityUid target, FormattedMessage message, bool getVerbs, bool centerAtCursor)
        {
            OpenTooltip(player, target, centerAtCursor, false);
            UpdateTooltipInfo(player, target, message);
        }

        /// <summary>
        ///     Opens the tooltip window and sets spriteview/name/etc, but does
        ///     not fill it with information. This is done when the server sends examine info/verbs,
        ///     or immediately if it's entirely clientside.
        /// </summary>
        public void OpenTooltip(EntityUid player, EntityUid target, bool centeredOnCursor=true, bool openAtOldTooltip=true, bool knowTarget = true)
        {
            // Close any examine tooltip that might already be opened
            // Before we do that, save its position. We'll prioritize opening any new popups there if
            // openAtOldTooltip is true.
            ScreenCoordinates? oldTooltipPos = _examineTooltipOpen != null ? _popupPos : null;
            CloseTooltip();

            // cache entity for Update function
            _examinedEntity = target;

            const float minWidth = 300;

            if (openAtOldTooltip && oldTooltipPos != null)
            {
                _popupPos = oldTooltipPos.Value;
            }
            else if (centeredOnCursor)
            {
                _popupPos = _userInterfaceManager.MousePositionScaled;
            }
            else
            {
                _popupPos = _eyeManager.CoordinatesToScreen(Transform(target).Coordinates);
                _popupPos = _userInterfaceManager.ScreenToUIPosition(_popupPos);
            }

            // Actually open the tooltip.
            _examineTooltipOpen = new Popup { MaxWidth = 400 };
            _userInterfaceManager.ModalRoot.AddChild(_examineTooltipOpen);
            var panel = new PanelContainer() { Name = "ExaminePopupPanel" };
            panel.AddStyleClass(StyleClassEntityTooltip);
            panel.ModulateSelfOverride = Color.LightGray.WithAlpha(0.90f);
            _examineTooltipOpen.AddChild(panel);

            var vBox = new BoxContainer
            {
                Name = "ExaminePopupVbox",
                Orientation = LayoutOrientation.Vertical
            };
            panel.AddChild(vBox);

            var hBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 5
            };

            vBox.AddChild(hBox);

            if (EntityManager.HasComponent<SpriteComponent>(target))
            {
                var spriteView = new SpriteView
                {
                    OverrideDirection = Direction.South,
                    SetSize = new Vector2(32, 32),
                    Margin = new Thickness(2, 0, 2, 0),
                };
                spriteView.SetEntity(target);
                hBox.AddChild(spriteView);
            }

            if (knowTarget)
            {
                hBox.AddChild(new Label
                {
                    Text = Identity.Name(target, EntityManager, player),
                    HorizontalExpand = true,
                });
            }
            else
            {
                hBox.AddChild(new Label
                {
                    Text = "???",
                    HorizontalExpand = true,
                });
            }

            panel.Measure(Vector2Helpers.Infinity);
            var size = Vector2.Max(new Vector2(minWidth, 0), panel.DesiredSize);

            _examineTooltipOpen.Open(UIBox2.FromDimensions(_popupPos.Position, size));
        }

        /// <summary>
        ///     Fills the examine tooltip with a message and buttons if applicable.
        /// </summary>
        public void UpdateTooltipInfo(EntityUid player, EntityUid target, FormattedMessage message, List<Verb>? verbs=null)
        {
            var vBox = _examineTooltipOpen?.GetChild(0).GetChild(0);
            if (vBox == null)
            {
                return;
            }

            foreach (var msg in message.Nodes)
            {
                if (msg.Name != null)
                    continue;

                var text = msg.Value.StringValue ?? "";

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var richLabel = new RichTextLabel() { Margin = new Thickness(4, 4, 0, 4)};
                richLabel.SetMessage(message);
                vBox.AddChild(richLabel);
                break;
            }

            verbs ??= new List<Verb>();
            var totalVerbs = _verbSystem.GetLocalVerbs(target, player, typeof(ExamineVerb));
            totalVerbs.UnionWith(verbs);

            AddVerbsToTooltip(totalVerbs);
        }

        private void AddVerbsToTooltip(IEnumerable<Verb> verbs)
        {
            if (_examineTooltipOpen == null)
                return;

            var buttonsHBox = new BoxContainer
            {
                Name = "ExamineButtonsHBox",
                Orientation = LayoutOrientation.Horizontal,
                HorizontalAlignment = Control.HAlignment.Right,
                VerticalAlignment = Control.VAlignment.Bottom,
            };

            // Examine button time
            foreach (var verb in verbs)
            {
                if (verb is not ExamineVerb examine)
                    continue;

                if (examine.Icon == null)
                    continue;

                if (!examine.ShowOnExamineTooltip)
                    continue;

                var button = new ExamineButton(examine);

                button.OnPressed += VerbButtonPressed;
                buttonsHBox.AddChild(button);
            }

            var vbox = _examineTooltipOpen?.GetChild(0).GetChild(0);
            if (vbox == null)
            {
                buttonsHBox.Dispose();
                return;
            }

            // Remove any existing buttons hbox, in case we generated it from the client
            // then received ones from the server
            var hbox = vbox.Children.Where(c => c.Name == "ExamineButtonsHBox").ToArray();
            if (hbox.Any())
            {
                vbox.Children.Remove(hbox.First());
            }
            vbox.AddChild(buttonsHBox);
        }

        public void VerbButtonPressed(BaseButton.ButtonEventArgs obj)
        {
            if (obj.Button is ExamineButton button)
            {
                _verbSystem.ExecuteVerb(_examinedEntity, button.Verb);
                if (button.Verb.CloseMenu ?? button.Verb.CloseMenuDefault)
                    CloseTooltip();
            }
        }

        public void DoExamine(EntityUid entity, bool centeredOnCursor = true, EntityUid? userOverride = null)
        {
            var playerEnt = userOverride ?? _playerManager.LocalPlayer?.ControlledEntity;
            if (playerEnt == null)
                return;

            FormattedMessage message;

            // Basically this just predicts that we can't make out the entity if we have poor vision.
            var canSeeClearly = !HasComp<BlurryVisionComponent>(playerEnt);

            OpenTooltip(playerEnt.Value, entity, centeredOnCursor, false, knowTarget: canSeeClearly);
            if (IsClientSide(entity)
                || _client.RunLevel == ClientRunLevel.SinglePlayerGame) // i.e. a replay
            {
                message = GetExamineText(entity, playerEnt);
                UpdateTooltipInfo(playerEnt.Value, entity, message);
            }
            else
            {
                // Ask server for extra examine info.
                if (entity != _lastExaminedEntity)
                    _idCounter += 1;
                if (_idCounter == int.MaxValue)
                    _idCounter = 0;
                RaiseNetworkEvent(new ExamineSystemMessages.RequestExamineInfoMessage(GetNetEntity(entity), _idCounter, true));
            }

            RaiseLocalEvent(entity, new ClientExaminedEvent(entity, playerEnt.Value));

            _lastExaminedEntity = entity;
        }

        private void CloseTooltip()
        {
            if (_examineTooltipOpen != null)
            {
                foreach (var control in _examineTooltipOpen.Children)
                {
                    if (control is ExamineButton button)
                    {
                        button.OnPressed -= VerbButtonPressed;
                    }
                }
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

    /// <summary>
    /// An entity was examined on the client.
    /// </summary>
    public sealed class ClientExaminedEvent : EntityEventArgs
    {
        /// <summary>
        ///     The entity performing the examining.
        /// </summary>
        public readonly EntityUid Examiner;

        /// <summary>
        ///     Entity being examined, for broadcast event purposes.
        /// </summary>
        public readonly EntityUid Examined;

        public ClientExaminedEvent(EntityUid examined, EntityUid examiner)
        {
            Examined = examined;
            Examiner = examiner;
        }
    }
}

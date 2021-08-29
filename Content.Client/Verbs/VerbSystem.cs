using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Content.Client.ContextMenu.UI;
using Content.Client.Resources;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Verbs
{
    [UsedImplicitly]
    public sealed class VerbSystem : SharedVerbSystem
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public event EventHandler<PointerInputCmdHandler.PointerInputCmdArgs>? ToggleContextMenu;
        public event EventHandler<bool>? ToggleContainerVisibility;

        private ContextMenuPresenter _contextMenuPresenter = default!;
        private VerbPopup? _currentVerbListRoot;
        private VerbPopup? _currentGroupList;
        private EntityUid _currentEntity;
        private List<Verb>? _currentVerbs;

        // TODO: Move presenter out of the system
        // TODO: Separate the rest of the UI from the logic
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeNetworkEvent<VerbsResponseMessage>(HandleVerbResponse);
            SubscribeNetworkEvent<PlayerContainerVisibilityMessage>(HandleContainerVisibilityMessage);

            _contextMenuPresenter = new ContextMenuPresenter(this);
            SubscribeLocalEvent<MoveEvent>(_contextMenuPresenter.HandleMoveEvent);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenContextMenu,
                    new PointerInputCmdHandler(HandleOpenContextMenu))
                .Register<VerbSystem>();
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _contextMenuPresenter?.Dispose();

            CommandBinds.Unregister<VerbSystem>();
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            ToggleContainerVisibility?.Invoke(this, false);
        }

        private bool HandleOpenContextMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (args.State == BoundKeyState.Down)
            {
                ToggleContextMenu?.Invoke(this, args);
            }
            return true;
        }
        private void HandleContainerVisibilityMessage(PlayerContainerVisibilityMessage ev)
        {
            ToggleContainerVisibility?.Invoke(this, ev.CanSeeThrough);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _contextMenuPresenter?.Update();
        }

        public void OpenContextMenu(IEntity entity, ScreenCoordinates screenCoordinates)
        {
            if (_currentVerbListRoot != null)
            {
                CloseVerbMenu();
            }

            _currentEntity = entity.Uid;
            _currentVerbListRoot = new VerbPopup();
            _userInterfaceManager.ModalRoot.AddChild(_currentVerbListRoot);
            _currentVerbListRoot.OnPopupHide += CloseVerbMenu;

            // Add text informing user that the verb list may be incomplete.
            if (!entity.Uid.IsClientSide())
            {
                _currentVerbListRoot.List.AddChild(new Label { Text = Loc.GetString("verb-system-waiting-on-server-text") });
            }

            // Get verb lists from client-side / shared systems
            var user = _playerManager.LocalPlayer!.ControlledEntity!;
            AssembleVerbsEvent assembleVerbs = new(user, entity, prepareGUI: true);
            RaiseLocalEvent(assembleVerbs);
            _currentVerbs = assembleVerbs.Verbs;

            // Show the menu
            FillVerbMenu();
            var box = UIBox2.FromDimensions(screenCoordinates.Position, (1, 1));
            _currentVerbListRoot.Open(box);

            //ask server for remaining verbs
            RaiseNetworkEvent(new RequestServerVerbsEvent(_currentEntity));
        }

        public void OnContextButtonPressed(IEntity entity)
        {
            OpenContextMenu(entity, _userInterfaceManager.MousePositionScaled);
        }

        private void HandleVerbResponse(VerbsResponseMessage msg)
        {
            if (_currentEntity != msg.Entity)
            {
                return;
            }

            if (_currentVerbs == null)
            {
                _currentVerbs = msg.Verbs;
            }
            else
            {
                // Merge message verbs with client side verb list
                foreach (var verb in msg.Verbs)
                {
                    _currentVerbs.Add(verb);
                }
            }

            // Clear currently shown verbs
            var vBox = _currentVerbListRoot!.List;
            vBox.DisposeAllChildren();

            // Show verbs, if there are any to show
            if (_currentVerbs.Count() > 0)
            {
                FillVerbMenu();
            }
            else
            {
                var panel = new PanelContainer();
                panel.AddChild(new Label { Text = Loc.GetString("verb-system-no-verbs-text") });
                vBox.AddChild(panel);
            }
        }

        private void FillVerbMenu()
        {
            if (!EntityManager.TryGetEntity(_currentEntity, out var entity) || _currentVerbs == null)
                return;

            _currentVerbs.Sort();

            DebugTools.AssertNotNull(_currentVerbListRoot);
            var vBox = _currentVerbListRoot!.List;

            HashSet<string> verbCategories = new();
            var first = true;
            foreach (var verb in _currentVerbs)
            {
                if (!first)
                {
                    vBox.AddChild(new PanelContainer
                    {
                        MinSize = (0, 2),
                        PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#333") }
                    });
                }
                first = false;

                // Does this verb not belong to a category?
                if (string.IsNullOrEmpty(verb.Category))
                {
                    vBox.AddChild(new VerbButton(this, verb, entity));
                }
                // Else, does this verb belong to a NEW verb category that hasn't already been added?
                else if (verbCategories.Add(verb.Category))
                {
                    // Create new verb group button
                    vBox.AddChild(
                        new VerbCategoryButton(this, verb.Category, verb.CategoryIcon, _currentVerbs, entity)
                        );
                }
            }
        }

        public void CloseVerbMenu()
        {
            _currentVerbListRoot?.Dispose();
            _currentVerbListRoot = null;
            _currentEntity = EntityUid.Invalid;
            _currentVerbs = null;
        }

        private void CloseAllMenus()
        {
            CloseVerbMenu();
            // CloseContextPopups();
            CloseGroupMenu();
        }

        public void CloseGroupMenu()
        {
            _currentGroupList?.Dispose();
            _currentGroupList = null;
        }

        private sealed class VerbPopup : Popup
        {
            public BoxContainer List { get; }

            public VerbPopup()
            {
                AddChild(new PanelContainer
                {
                    Children = {(List = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical
                    })},
                    PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#111E")}
                });
            }
        }

        private sealed class VerbButton : BaseButton
        {
            private readonly RichTextLabel _label;
            private readonly TextureRect _icon;

            public Texture? Icon
            {
                get => _icon.Texture;
                set => _icon.Texture = value;
            }

            public VerbButton(VerbSystem system, Verb verb, IEntity owner)
            {
                AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        (_icon = new TextureRect
                        {
                            MinSize = (32, 32),
                            Stretch = TextureRect.StretchMode.KeepCentered,
                            TextureScale = (0.5f, 0.5f)
                        }),
                        (_label = new RichTextLabel()),
                        // Padding
                        new Control {MinSize = (8, 0)}
                    }
                });

                _label.SetMessage(FormattedMessage.FromMarkupPermissive(verb.Text));
                Disabled = verb.IsDisabled;

                if (verb.Icon != null)
                {
                    Icon = verb.Icon.Frame0();
                }

                if (!verb.IsDisabled)
                {
                    OnPressed += _ =>
                    {
                        system.CloseAllMenus();
                        try
                        {
                            if (verb.Act != null)
                            {
                                // verb was defined client-side (e.g., examine)
                                // May still end up raising network events.
                                verb.Act();
                            }
                            else
                            {
                                // Verb was defined server-side.
                                system.RaiseNetworkEvent(new UseVerbMessage(owner.Uid, verb.Key));
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.ErrorS("verb", "Exception in verb {0} on {1}:\n{2}", verb.Key, owner.ToString(), e);
                        }
                    };
                }
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);

                if (DrawMode == DrawModeEnum.Hover)
                {
                    handle.DrawRect(PixelSizeBox, Color.DarkSlateGray);
                }
            }
        }

        private sealed class VerbCategoryButton : Control
        {
            private static readonly TimeSpan HoverDelay = TimeSpan.FromSeconds(0.2);

            private readonly VerbSystem _system;

            private readonly Label _label;
            private readonly TextureRect _icon;

            private CancellationTokenSource? _openCancel;

            private readonly IEntity _owner;

            private readonly IEnumerable<Verb> _verbs;

            public string? Text
            {
                get => _label.Text;
                set => _label.Text = value;
            }

            public Texture? Icon
            {
                get => _icon.Texture;
                set => _icon.Texture = value;
            }

            public VerbCategoryButton(VerbSystem system, string category, SpriteSpecifier? icon, List<Verb> verbs, IEntity owner)
            {
                _system = system;
                _owner = owner;
                Text = category;
                _verbs = verbs.Where(verb => verb.Category == category);

                MouseFilter = MouseFilterMode.Stop;

                AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        (_icon = new TextureRect
                        {
                            MinSize = (32, 32),
                            TextureScale = (0.5f, 0.5f),
                            Stretch = TextureRect.StretchMode.KeepCentered
                        }),

                        (_label = new Label
                        {
                            SizeFlagsHorizontal = SizeFlags.FillExpand
                        }),

                        // Padding
                        new Control {MinSize = (8, 0)},

                        new TextureRect
                        {
                            Texture = IoCManager.Resolve<IResourceCache>()
                                .GetTexture("/Textures/Interface/VerbIcons/group.svg.192dpi.png"),
                            TextureScale = (0.5f, 0.5f),
                            Stretch = TextureRect.StretchMode.KeepCentered,
                        }
                    }
                });

                if (icon != null)
                {
                    _icon.Texture = icon.Frame0();
                }
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);

                if (this == UserInterfaceManager.CurrentlyHovered)
                {
                    handle.DrawRect(PixelSizeBox, Color.DarkSlateGray);
                }
            }

            protected override void MouseEntered()
            {
                base.MouseEntered();

                _openCancel = new CancellationTokenSource();

                Timer.Spawn(HoverDelay, () =>
                {
                    if (_system._currentGroupList != null)
                    {
                        _system.CloseGroupMenu();
                    }

                    var popup = _system._currentGroupList = new VerbPopup();

                    var first = true;
                    foreach (var verb in _verbs)
                    {
                        if (!first)
                        {
                            popup.List.AddChild(new PanelContainer
                            {
                                MinSize = (0, 2),
                                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#333")}
                            });
                        }

                        first = false;

                        popup.List.AddChild(new VerbButton(_system, verb, _owner));
                    }

                    UserInterfaceManager.ModalRoot.AddChild(popup);
                    popup.Open(UIBox2.FromDimensions(GlobalPosition + (Width, 0), (1, 1)), GlobalPosition);
                }, _openCancel.Token);
            }

            protected override void MouseExited()
            {
                base.MouseExited();

                _openCancel?.Cancel();
                _openCancel = null;
            }
        }
    }
}

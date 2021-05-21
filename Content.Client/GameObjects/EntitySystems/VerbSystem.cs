using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Content.Client.Utility;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.GameTicking;
using Content.Shared.Input;
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
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class VerbSystem : SharedVerbSystem, IResettingEntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

        public event EventHandler<PointerInputCmdHandler.PointerInputCmdArgs>? ToggleContextMenu;
        public event EventHandler<bool>? ToggleContainerVisibility;

        private ContextMenuPresenter _contextMenuPresenter = default!;
        private VerbPopup? _currentVerbListRoot;
        private VerbPopup? _currentGroupList;
        private EntityUid _currentEntity;

        // TODO: Move presenter out of the system
        // TODO: Separate the rest of the UI from the logic
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<VerbSystemMessages.VerbsResponseMessage>(FillEntityPopup);
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

            UnsubscribeNetworkEvent<VerbSystemMessages.VerbsResponseMessage>();
            UnsubscribeNetworkEvent<PlayerContainerVisibilityMessage>();
            UnsubscribeLocalEvent<MoveEvent>();
            _contextMenuPresenter?.Dispose();

            CommandBinds.Unregister<VerbSystem>();
        }

        public void Reset()
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

            if (!entity.Uid.IsClientSide())
            {
                _currentVerbListRoot.List.AddChild(new Label { Text = Loc.GetString("Waiting on Server...") });
                RaiseNetworkEvent(new VerbSystemMessages.RequestVerbsMessage(_currentEntity));
            }

            var box = UIBox2.FromDimensions(screenCoordinates.Position, (1, 1));
            _currentVerbListRoot.Open(box);
        }

        public void OnContextButtonPressed(IEntity entity)
        {
            OpenContextMenu(entity, _userInterfaceManager.MousePositionScaled);
        }

        private void FillEntityPopup(VerbSystemMessages.VerbsResponseMessage msg)
        {
            if (_currentEntity != msg.Entity || !EntityManager.TryGetEntity(_currentEntity, out var entity))
            {
                return;
            }

            DebugTools.AssertNotNull(_currentVerbListRoot);

            var buttons = new Dictionary<string, List<ListedVerbData>>();
            var groupIcons = new Dictionary<string, SpriteSpecifier>();

            var vBox = _currentVerbListRoot!.List;
            vBox.DisposeAllChildren();

            // Local variable so that scope capture ensures this is the correct value.
            var curEntity = _currentEntity;

            foreach (var data in msg.Verbs)
            {
                var list = buttons.GetOrNew(data.Category);

                if (data.CategoryIcon != null && !groupIcons.ContainsKey(data.Category))
                {
                    groupIcons.Add(data.Category, data.CategoryIcon);
                }

                list.Add(new ListedVerbData(data.Text, !data.Available, data.Key, entity.ToString()!, () =>
                {
                    RaiseNetworkEvent(new VerbSystemMessages.UseVerbMessage(curEntity, data.Key));
                    CloseAllMenus();
                }, data.Icon));
            }

            var user = GetUserEntity();
            //Get verbs, component dependent.
            foreach (var (component, verb) in VerbUtility.GetVerbs(entity))
            {
                if (!VerbUtility.VerbAccessChecks(user, entity, verb))
                {
                    continue;
                }

                var verbData = verb.GetData(user, component);

                if (verbData.IsInvisible)
                    continue;

                var list = buttons.GetOrNew(verbData.Category);

                if (verbData.CategoryIcon != null && !groupIcons.ContainsKey(verbData.Category))
                {
                    groupIcons.Add(verbData.Category, verbData.CategoryIcon);
                }

                list.Add(new ListedVerbData(verbData.Text, verbData.IsDisabled, verb.ToString()!, entity.ToString()!,
                    () => verb.Activate(user, component), verbData.Icon));
            }

            //Get global verbs. Visible for all entities regardless of their components.
            foreach (var globalVerb in VerbUtility.GetGlobalVerbs(Assembly.GetExecutingAssembly()))
            {
                if (!VerbUtility.VerbAccessChecks(user, entity, globalVerb))
                {
                    continue;
                }

                var verbData = globalVerb.GetData(user, entity);

                if (verbData.IsInvisible)
                    continue;

                var list = buttons.GetOrNew(verbData.Category);

                if (verbData.CategoryIcon != null && !groupIcons.ContainsKey(verbData.Category))
                {
                    groupIcons.Add(verbData.Category, verbData.CategoryIcon);
                }

                list.Add(new ListedVerbData(verbData.Text, verbData.IsDisabled, globalVerb.ToString()!,
                    entity.ToString()!,
                    () => globalVerb.Activate(user, entity), verbData.Icon));
            }

            if (buttons.Count > 0)
            {
                var first = true;
                foreach (var (category, verbs) in buttons)
                {
                    if (string.IsNullOrEmpty(category))
                        continue;

                    if (!first)
                    {
                        vBox.AddChild(new PanelContainer
                        {
                            MinSize = (0, 2),
                            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#333") }
                        });
                    }

                    first = false;

                    groupIcons.TryGetValue(category, out var icon);

                    vBox.AddChild(CreateCategoryButton(category, verbs, icon));
                }

                if (buttons.ContainsKey(""))
                {
                    buttons[""].Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.CurrentCulture));

                    foreach (var verb in buttons[""])
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

                        vBox.AddChild(CreateVerbButton(verb));
                    }
                }
            }
            else
            {
                var panel = new PanelContainer();
                panel.AddChild(new Label { Text = Loc.GetString("No verbs!") });
                vBox.AddChild(panel);
            }
        }

        private VerbButton CreateVerbButton(ListedVerbData data)
        {
            var button = new VerbButton
            {
                Text = Loc.GetString(data.Text),
                Disabled = data.Disabled
            };

            if (data.Icon != null)
            {
                button.Icon = data.Icon.Frame0();
            }

            if (!data.Disabled)
            {
                button.OnPressed += _ =>
                {
                    CloseAllMenus();
                    try
                    {
                        data.Action.Invoke();
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("verb", "Exception in verb {0} on {1}:\n{2}", data.VerbName, data.OwnerName, e);
                    }
                };
            }

            return button;
        }

        private Control CreateCategoryButton(string text, List<ListedVerbData> verbButtons, SpriteSpecifier? icon)
        {
            verbButtons.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.CurrentCulture));

            return new VerbGroupButton(this, verbButtons, icon)
            {
                Text = Loc.GetString(text),
            };
        }

        public void CloseVerbMenu()
        {
            _currentVerbListRoot?.Dispose();
            _currentVerbListRoot = null;
            _currentEntity = EntityUid.Invalid;
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

        private IEntity GetUserEntity()
        {
            return _playerManager.LocalPlayer!.ControlledEntity!;
        }

        private sealed class VerbPopup : Popup
        {
            public VBoxContainer List { get; }

            public VerbPopup()
            {
                AddChild(new PanelContainer
                {
                    Children = {(List = new VBoxContainer())},
                    PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#111E")}
                });
            }
        }

        private sealed class VerbButton : BaseButton
        {
            private readonly Label _label;
            private readonly TextureRect _icon;

            public Texture? Icon
            {
                get => _icon.Texture;
                set => _icon.Texture = value;
            }

            public string? Text
            {
                get => _label.Text;
                set => _label.Text = value;
            }

            public VerbButton()
            {
                AddChild(new HBoxContainer
                {
                    Children =
                    {
                        (_icon = new TextureRect
                        {
                            MinSize = (32, 32),
                            Stretch = TextureRect.StretchMode.KeepCentered,
                            TextureScale = (0.5f, 0.5f)
                        }),
                        (_label = new Label()),
                        // Padding
                        new Control {MinSize = (8, 0)}
                    }
                });
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

        private sealed class VerbGroupButton : Control
        {
            private static readonly TimeSpan HoverDelay = TimeSpan.FromSeconds(0.2);

            private readonly VerbSystem _system;

            private readonly Label _label;
            private readonly TextureRect _icon;

            private CancellationTokenSource? _openCancel;

            public List<ListedVerbData> VerbButtons { get; }

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

            public VerbGroupButton(VerbSystem system, List<ListedVerbData> verbButtons, SpriteSpecifier? icon)
            {
                _system = system;
                VerbButtons = verbButtons;

                MouseFilter = MouseFilterMode.Stop;

                AddChild(new HBoxContainer
                {
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
                    foreach (var verb in VerbButtons)
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

                        popup.List.AddChild(_system.CreateVerbButton(verb));
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

        private sealed class ListedVerbData
        {
            public string Text { get; }
            public bool Disabled { get; }
            public string VerbName { get; }
            public string OwnerName { get; }
            public SpriteSpecifier? Icon { get; }
            public Action Action { get; }

            public ListedVerbData(string text, bool disabled, string verbName, string ownerName,
                Action action, SpriteSpecifier? icon)
            {
                Text = text;
                Disabled = disabled;
                VerbName = verbName;
                OwnerName = ownerName;
                Action = action;
                Icon = icon;
            }
        }
    }
}

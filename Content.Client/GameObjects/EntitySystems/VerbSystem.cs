using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Content.Client.State;
using Content.Client.UserInterface;
using Content.Client.Utility;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.State;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class VerbSystem : EntitySystem
    {
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        private EntityList _currentEntityList;
        private VerbPopup _currentVerbListRoot;
        private VerbPopup _currentGroupList;

        private EntityUid _currentEntity;

        private bool IsAnyContextMenuOpen => _currentEntityList != null || _currentVerbListRoot != null;


        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<VerbSystemMessages.VerbsResponseMessage>(FillEntityPopup);

            IoCManager.InjectDependencies(this);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenContextMenu,
                    new PointerInputCmdHandler(OnOpenContextMenu))
                .Register<VerbSystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<VerbSystem>();
            base.Shutdown();
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
                _currentVerbListRoot.List.AddChild(new Label {Text = "Waiting on Server..."});
                RaiseNetworkEvent(new VerbSystemMessages.RequestVerbsMessage(_currentEntity));
            }

            var box = UIBox2.FromDimensions(screenCoordinates.Position, (1, 1));
            _currentVerbListRoot.Open(box);
        }

        private bool OnOpenContextMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (IsAnyContextMenuOpen)
            {
                CloseAllMenus();
                return true;
            }

            if (!(_stateManager.CurrentState is GameScreenBase gameScreen))
            {
                return false;
            }

            var mapCoordinates = args.Coordinates.ToMap(_mapManager);
            var entities = _entityManager.GetEntitiesIntersecting(mapCoordinates.MapId,
                Box2.CenteredAround(mapCoordinates.Position, (0.5f, 0.5f))).ToList();

            if (entities.Count == 0)
            {
                return false;
            }

            _currentEntityList = new EntityList();
            _currentEntityList.OnPopupHide += CloseAllMenus;
            var first = true;
            foreach (var entity in entities)
            {
                if (!entity.TryGetComponent(out ISpriteComponent sprite) || !sprite.Visible)
                {
                    continue;
                }

                if (ContainerHelpers.TryGetContainer(entity, out var container) && !container.ShowContents)
                {
                    continue;
                }

                if (!first)
                {
                    _currentEntityList.List.AddChild(new PanelContainer
                    {
                        CustomMinimumSize = (0, 2),
                        PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#333")}
                    });
                }

                var debugEnabled = _userInterfaceManager.DebugMonitors.Visible;
                _currentEntityList.List.AddChild(new EntityButton(this, entity, debugEnabled));
                first = false;
            }

            _userInterfaceManager.ModalRoot.AddChild(_currentEntityList);

            var size = _currentEntityList.List.CombinedMinimumSize;
            var box = UIBox2.FromDimensions(args.ScreenCoordinates.Position, size);
            _currentEntityList.Open(box);

            return true;
        }

        private void OnContextButtonPressed(IEntity entity)
        {
            OpenContextMenu(entity, new ScreenCoordinates(_inputManager.MouseScreenPosition));
        }

        private void FillEntityPopup(VerbSystemMessages.VerbsResponseMessage msg)
        {
            if (_currentEntity != msg.Entity || !_entityManager.TryGetEntity(_currentEntity, out var entity))
            {
                return;
            }

            DebugTools.AssertNotNull(_currentVerbListRoot);

            var buttons = new Dictionary<string, List<ListedVerbData>>();
            var groupIcons = new Dictionary<string, SpriteSpecifier>();

            var vBox = _currentVerbListRoot.List;
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

                list.Add(new ListedVerbData(data.Text, !data.Available, data.Key, entity.ToString(), () =>
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

                list.Add(new ListedVerbData(verbData.Text, verbData.IsDisabled, verb.ToString(), entity.ToString(),
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

                list.Add(new ListedVerbData(verbData.Text, verbData.IsDisabled, globalVerb.ToString(),
                    entity.ToString(),
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
                            CustomMinimumSize = (0, 2),
                            PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#333")}
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
                                CustomMinimumSize = (0, 2),
                                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#333")}
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
                panel.AddChild(new Label {Text = "No verbs!"});
                vBox.AddChild(panel);
            }
        }

        private VerbButton CreateVerbButton(ListedVerbData data)
        {
            var button = new VerbButton
            {
                Text = data.Text,
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

        private Control CreateCategoryButton(string text, List<ListedVerbData> verbButtons, SpriteSpecifier icon)
        {
            verbButtons.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.CurrentCulture));

            return new VerbGroupButton(this, verbButtons, icon)
            {
                Text = text,
            };
        }

        private void CloseVerbMenu()
        {
            _currentVerbListRoot?.Dispose();
            _currentVerbListRoot = null;
            _currentEntity = EntityUid.Invalid;
        }

        private void CloseEntityList()
        {
            _currentEntityList?.Dispose();
            _currentEntityList = null;
        }

        private void CloseAllMenus()
        {
            CloseVerbMenu();
            CloseEntityList();
            CloseGroupMenu();
        }

        private void CloseGroupMenu()
        {
            _currentGroupList?.Dispose();
            _currentGroupList = null;
        }

        private IEntity GetUserEntity()
        {
            return _playerManager.LocalPlayer.ControlledEntity;
        }

        private sealed class EntityList : Popup
        {
            public VBoxContainer List { get; }

            public EntityList()
            {
                AddChild(new PanelContainer
                {
                    Children = {(List = new VBoxContainer())},
                    PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#111E")}
                });
            }
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

        private sealed class EntityButton : Control
        {
            private readonly VerbSystem _master;
            private readonly IEntity _entity;

            public EntityButton(VerbSystem master, IEntity entity, bool showUid)
            {
                _master = master;
                _entity = entity;

                MouseFilter = MouseFilterMode.Stop;

                var control = new HBoxContainer {SeparationOverride = 6};
                if (entity.TryGetComponent(out ISpriteComponent sprite))
                {
                    control.AddChild(new SpriteView {Sprite = sprite});
                }

                var text = entity.Name;
                if (showUid)
                {
                    text = $"{text} ({entity.Uid})";
                }
                control.AddChild(new MarginContainer
                {
                    MarginLeftOverride = 4,
                    MarginRightOverride = 4,
                    Children = {new Label {Text = text}}
                });

                AddChild(control);
            }

            protected override void KeyBindDown(GUIBoundKeyEventArgs args)
            {
                base.KeyBindDown(args);

                if (args.Function == ContentKeyFunctions.OpenContextMenu)
                {
                    _master.OnContextButtonPressed(_entity);
                    return;
                }

                if (args.Function == EngineKeyFunctions.Use ||
                    args.Function == ContentKeyFunctions.Point ||
                    args.Function == ContentKeyFunctions.TryPullObject ||
                    args.Function == ContentKeyFunctions.MovePulledObject)
                {
                    // TODO: Remove an entity from the menu when it is deleted
                    if (_entity.Deleted)
                    {
                        _master.CloseAllMenus();
                        return;
                    }

                    var inputSys = _master.EntitySystemManager.GetEntitySystem<InputSystem>();

                    var func = args.Function;
                    var funcId = _master._inputManager.NetworkBindMap.KeyFunctionID(args.Function);

                    var message = new FullInputCmdMessage(_master._gameTiming.CurTick, _master._gameTiming.TickFraction, funcId, BoundKeyState.Down,
                        _entity.Transform.GridPosition,
                        args.PointerLocation, _entity.Uid);

                    // client side command handlers will always be sent the local player session.
                    var session = _master._playerManager.LocalPlayer.Session;
                    inputSys.HandleInputCommand(session, func, message);

                    _master.CloseAllMenus();
                    return;
                }

                if (args.Function == ContentKeyFunctions.ExamineEntity)
                {
                    Get<ExamineSystem>().DoExamine(_entity);
                    return;
                }

                if (_master._itemSlotManager.OnButtonPressed(args, _entity))
                {
                    _master.CloseAllMenus();
                }
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);

                if (UserInterfaceManager.CurrentlyHovered == this)
                {
                    handle.DrawRect(PixelSizeBox, Color.DarkSlateGray);
                }
            }
        }

        private sealed class VerbButton : BaseButton
        {
            private readonly Label _label;
            private readonly TextureRect _icon;

            public Texture Icon
            {
                get => _icon.Texture;
                set => _icon.Texture = value;
            }

            public string Text
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
                            CustomMinimumSize = (32, 32),
                            Stretch = TextureRect.StretchMode.KeepCentered
                        }),
                        (_label = new Label()),
                        // Padding
                        new Control {CustomMinimumSize = (8, 0)}
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
            public List<ListedVerbData> VerbButtons { get; }
            private readonly Label _label;
            private readonly TextureRect _icon;

            private CancellationTokenSource _openCancel;

            public string Text
            {
                get => _label.Text;
                set => _label.Text = value;
            }

            public Texture Icon
            {
                get => _icon.Texture;
                set => _icon.Texture = value;
            }

            public VerbGroupButton(VerbSystem system, List<ListedVerbData> verbButtons, SpriteSpecifier icon)
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
                            CustomMinimumSize = (32, 32),
                            Stretch = TextureRect.StretchMode.KeepCentered
                        }),

                        (_label = new Label
                        {
                            SizeFlagsHorizontal = SizeFlags.FillExpand
                        }),

                        // Padding
                        new Control {CustomMinimumSize = (8, 0)},

                        new TextureRect
                        {
                            Texture = IoCManager.Resolve<IResourceCache>()
                                .GetTexture("/Textures/Interface/VerbIcons/group.svg.96dpi.png"),
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
                                CustomMinimumSize = (0, 2),
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
            public SpriteSpecifier Icon { get; }
            public Action Action { get; }

            public ListedVerbData(string text, bool disabled, string verbName, string ownerName,
                Action action, SpriteSpecifier icon)
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

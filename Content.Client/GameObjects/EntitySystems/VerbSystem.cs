using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Content.Client.State;
using Content.Client.UserInterface;
using Content.Client.Utility;
using Content.Shared;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed partial class VerbSystem : SharedVerbSystem, IResettingEntitySystem
    {
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private MapCoordinates _clickedHere;
        private Dictionary<IEntity, ContextMenuElement> _entityMenuElements;
        private Stack<ContextMenuPopup> _stackContextMenus = new();
        private CancellationTokenSource _cancellationTokenSource;

        private VerbPopup _currentVerbListRoot;
        private VerbPopup _currentGroupList;

        private EntityUid _currentEntity;

        private bool _playerCanSeeThroughContainers;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<VerbSystemMessages.VerbsResponseMessage>(FillEntityPopup);
            SubscribeNetworkEvent<PlayerContainerVisibilityMessage>(HandleContainerVisibilityMessage);

            SubscribeLocalEvent<MoveEvent>(HandleMoveEvent);

            _cfg.OnValueChanged(CCVars.ContextMenuGroupingType, OnGroupingContextMenuChanged, true);

            IoCManager.InjectDependencies(this);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenContextMenu,
                    new PointerInputCmdHandler(OnOpenContextMenu))
                .Register<VerbSystem>();
        }

        public override void Shutdown()
        {
            UnsubscribeLocalEvent<MoveEvent>();

            CommandBinds.Unregister<VerbSystem>();
            base.Shutdown();
        }

        public void Reset()
        {
            _playerCanSeeThroughContainers = false;
        }

        private bool IsAnyContextMenuOpen()
        {
            return _currentVerbListRoot != null || _stackContextMenus.Count > 0;
        }

        private void HandleContainerVisibilityMessage(PlayerContainerVisibilityMessage ev)
        {
            _playerCanSeeThroughContainers = ev.CanSeeThrough;
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

        public bool CanSeeOnContextMenu(IEntity entity)
        {
            if (!entity.TryGetComponent(out SpriteComponent sprite) || !sprite.Visible)
            {
                return false;
            }

            if (entity.GetAllComponents<IShowContextMenu>().Any(s => !s.ShowContextMenu(entity)))
            {
                return false;
            }

            if (!_playerCanSeeThroughContainers &&
                entity.TryGetContainer(out var container) &&
                !container.ShowContents)
            {
                return false;
            }

            return true;
        }

        private void RemoveFromUI(IEntity entity)
        {
            var contextMenuElement = _entityMenuElements[entity];
            _entityMenuElements.Remove(entity);

            var parent = contextMenuElement.ParentMenu;
            parent.RemoveEntityFrom(contextMenuElement, entity);
            if (_entityMenuElements.Count == 0)
            {
                CloseContextPopups();
            }
        }

        private void AddToUI(IEntity e, ContextMenuElement control)
        {
            if (_entityMenuElements.ContainsKey(e))
            {
                _entityMenuElements[e] = control;
            }
            else
            {
                _entityMenuElements.Add(e, control);
            }
        }

        private void HandleMoveEvent(MoveEvent ev)
        {
            if (_entityMenuElements == null || _entityMenuElements.Count == 0) return;

            var entity = ev.Sender;
            if (_entityMenuElements.ContainsKey(entity))
            {
                if (!entity.Transform.MapPosition.InRange(_clickedHere, 1.0f))
                {
                    RemoveFromUI(entity);
                }
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (_entityMenuElements == null || _entityMenuElements.Count == 0) return;

            foreach (var (k, v) in _entityMenuElements)
            {
                if (k.Deleted || k.IsInContainer())
                {
                    RemoveFromUI(k);
                }
            }
        }

        private bool OnOpenContextMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (IsAnyContextMenuOpen())
            {
                CloseAllMenus();
                return true;
            }

            if (_stateManager.CurrentState is not GameScreenBase)
            {
                return false;
            }

            var mapCoordinates = args.Coordinates.ToMap(EntityManager);
            var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;

            if (playerEntity == null || !TryGetContextEntities(playerEntity, mapCoordinates, out var entities))
            {
                return false;
            }

            _clickedHere = mapCoordinates;

            entities = entities.Where(e => CanSeeOnContextMenu(e) && e.Prototype != null).ToList();
            var entitySpriteStates = GroupEntities(entities);

            if (entitySpriteStates.Count == 0)
            {
                return false;
            }

            var orderedStates = entitySpriteStates.ToList();
            orderedStates.Sort((x, y) =>
                string.CompareOrdinal(x.First().Prototype!.Name, y.First().Prototype!.Name));

            _entityMenuElements = new();
            _stackContextMenus = new();

            var rootContextMenu = new ContextMenuPopup();
            rootContextMenu.OnPopupHide += CloseAllMenus;

            var debugEnabled = _userInterfaceManager.DebugMonitors.Visible;
            rootContextMenu.FillContextMenuPopup(orderedStates, null, this, debugEnabled);

            _stackContextMenus.Push(rootContextMenu);
            _userInterfaceManager.ModalRoot.AddChild(rootContextMenu);

            var size = rootContextMenu.List.CombinedMinimumSize;
            var box = UIBox2.FromDimensions(_userInterfaceManager.MousePositionScaled, size);
            rootContextMenu.Open(box);

            return true;
        }

        public void OnContextButtonPressed(IEntity entity)
        {
            OpenContextMenu(entity, new ScreenCoordinates(_userInterfaceManager.MousePositionScaled));
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
                                CustomMinimumSize = (0, 2),
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

        private Control CreateCategoryButton(string text, List<ListedVerbData> verbButtons, SpriteSpecifier icon)
        {
            verbButtons.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.CurrentCulture));

            return new VerbGroupButton(this, verbButtons, icon)
            {
                Text = Loc.GetString(text),
            };
        }

        private void CloseVerbMenu()
        {
            _currentVerbListRoot?.Dispose();
            _currentVerbListRoot = null;
            _currentEntity = EntityUid.Invalid;
        }

        private void CloseContextPopups()
        {
            while (_stackContextMenus.Count > 0)
            {
                _stackContextMenus.Pop()?.Dispose();
            }
            _entityMenuElements?.Clear();
            _entityMenuElements = null;
        }

        private void CloseContextPopups(int depth)
        {
            while (_stackContextMenus.Count > 0 && _stackContextMenus.Peek().Depth > depth)
            {
                _stackContextMenus.Pop()?.Dispose();
            }
        }

        private void CloseAllMenus()
        {
            CloseVerbMenu();
            CloseContextPopups();
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

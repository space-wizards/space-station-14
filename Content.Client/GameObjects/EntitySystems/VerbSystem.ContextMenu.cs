using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Content.Client.GameObjects.Components;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Client.GameObjects.EntitySystems
{
    public sealed partial class VerbSystem
    {
        private abstract class ContextMenuElement : Control
        {
            protected readonly VerbSystem VSystem;
            protected readonly int DepthLevel;
            protected readonly bool IsDebug;

            public ContextMenuElement(VerbSystem verbSystem, int depth, bool isDebug)
            {
                VSystem = verbSystem;
                DepthLevel = depth;
                IsDebug = isDebug;

                MouseFilter = MouseFilterMode.Stop;
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);

                if (UserInterfaceManager.CurrentlyHovered == this)
                {
                    handle.DrawRect(PixelSizeBox, Color.DarkSlateGray);
                }
            }

            protected void ContextKeyBind(GUIBoundKeyEventArgs args, IEntity entity)
            {
            }

            protected abstract void InitializeControl();
        }

        private sealed class SingleC : ContextMenuElement
        {
            private SpriteComponent _sprite;
            private readonly IEntity _entity;
            private InteractionOutlineComponent _outline;

            public SingleC(VerbSystem verbSystem, IEntity entity, int depth, bool isDebug) : base(verbSystem, depth, isDebug)
            {
                _entity = entity;
                InitializeControl();

                _entity.EntityManager.EventBus.SubscribeEvent(EventSource.Local, Move);
            }

            protected override void InitializeControl()
            {
                AddChild(
                    new HBoxContainer
                    {
                        SeparationOverride = 6,
                        Children =
                        {
                            new LayoutContainer
                            {
                                Children =
                                {
                                    new SpriteView { Sprite = _entity.GetComponent<ISpriteComponent>() }
                                }
                            },
                            new MarginContainer
                            {
                                MarginLeftOverride = 4,
                                MarginRightOverride = 4,
                                Children =
                                {
                                    new Label { Text = Loc.GetString(IsDebug ? $"{_entity.Name} ({_entity.Uid})" : _entity.Name) }
                                }
                            },
                        }
                    }
                    );
            }
        }

        private sealed class MultiC : ContextMenuElement
        {
            private SpriteComponent _sprite;
            private readonly List<IEntity> _entities;
            private InteractionOutlineComponent _outline;

            public MultiC(VerbSystem verbSystem, IEnumerable<IEntity> entities, int depth, bool isDebug) : base(verbSystem, depth, isDebug)
            {
                _entities = new(entities);
                InitializeControl();

                foreach (var e in _entities)
                {
                    e.EntityManager.EventBus.RaiseEvent(EventSource.Local, new EntityDeletedMessage);
                    //            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new IconSmoothDirtyEvent(Owner,null, SnapGrid.Offset, Mode));

                }

                SubscribeLocalEvent<MoveEvent>(HandleMove);

            }

            protected override void InitializeControl()
            {
                var entity = _entities.First();

                var labelCount = new Label
                {
                    Text = Loc.GetString(_entities.Count.ToString()),
                    StyleClasses = { StyleNano.StyleClassContextMenuCount },
                };
                LayoutContainer.SetAnchorPreset(labelCount, LayoutContainer.LayoutPreset.BottomRight);
                LayoutContainer.SetGrowHorizontal(labelCount, LayoutContainer.GrowDirection.Begin);
                LayoutContainer.SetGrowVertical(labelCount, LayoutContainer.GrowDirection.Begin);

                AddChild(
                    new HBoxContainer()
                    {
                        SeparationOverride = 6,
                        Children =
                        {
                            new LayoutContainer
                            {
                                Children =
                                {
                                    new SpriteView { Sprite = entity.GetComponent<ISpriteComponent>() },
                                    labelCount
                                }
                            },
                            new MarginContainer
                            {
                                MarginLeftOverride = 4,
                                MarginRightOverride = 4,
                                Children =
                                {
                                    new Label { Text = Loc.GetString(entity.Name) }
                                }
                            },
                            new TextureRect
                            {
                                Texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Interface/VerbIcons/group.svg.96dpi.png"),
                                Stretch = TextureRect.StretchMode.KeepCentered,
                            }
                        }
                    }
                );
            }

            protected override void MouseEntered()
            {
                base.MouseEntered();
                if (VSystem._currentGroupList != null)
                {
                    VSystem.CloseGroupMenu();
                }

                if (VSystem._contextPopupExt != null)
                {
                    VSystem.CloseContextPopupExt();
                }


            }
        }




        private abstract class ContextElement : Control
        {
            protected readonly VerbSystem VSystem;
            protected readonly bool ShowUid;
            protected readonly bool IsRoot;
            public MarginContainer Margin { get; set; }

            public ContextElement(VerbSystem system, bool showUid, bool isRoot)
            {
                VSystem = system;
                ShowUid = showUid;
                IsRoot = isRoot;

                MouseFilter = MouseFilterMode.Stop;

                Margin = new MarginContainer
                {
                    MarginLeftOverride = 4,
                    MarginRightOverride = 4,
                };
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);

                if (UserInterfaceManager.CurrentlyHovered == this)
                {
                    handle.DrawRect(PixelSizeBox, Color.DarkSlateGray);
                }
            }

            protected void ContextKeyBindDown(GUIBoundKeyEventArgs args, IEntity entity, bool isSingle = true)
            {
                if (args.Function == ContentKeyFunctions.OpenContextMenu && isSingle)
                {
                    VSystem.OnContextButtonPressed(entity);
                    return;
                }

                if (args.Function == ContentKeyFunctions.ExamineEntity && isSingle)
                {
                    Get<ExamineSystem>().DoExamine(entity);
                    return;
                }

                if (args.Function == EngineKeyFunctions.Use ||
                    args.Function == ContentKeyFunctions.Point ||
                    args.Function == ContentKeyFunctions.TryPullObject ||
                    args.Function == ContentKeyFunctions.MovePulledObject)
                {
                    // TODO: Remove an entity from the menu when it is deleted
                    if (entity.Deleted)
                    {
                        VSystem.CloseAllMenus();
                        return;
                    }

                    var inputSys = VSystem.EntitySystemManager.GetEntitySystem<InputSystem>();

                    var func = args.Function;
                    var funcId = VSystem._inputManager.NetworkBindMap.KeyFunctionID(args.Function);

                    var message = new FullInputCmdMessage(VSystem._gameTiming.CurTick, VSystem._gameTiming.TickFraction, funcId,
                        BoundKeyState.Down, entity.Transform.Coordinates, args.PointerLocation, entity.Uid);

                    // client side command handlers will always be sent the local player session.
                    var session = VSystem._playerManager.LocalPlayer.Session;
                    inputSys.HandleInputCommand(session, func, message);

                    VSystem.CloseAllMenus();
                    return;
                }

                if (VSystem._itemSlotManager.OnButtonPressed(args, entity))
                {
                    VSystem.CloseAllMenus();
                }
            }
        }

        private sealed class SingleContextElement : ContextElement
        {
            private readonly IEntity _entity;
            private InteractionOutlineComponent _outline;
            private SpriteComponent _sprite;

            private bool _drawOutline = false;
            private int _oldDrawDepth;

            public SingleContextElement(VerbSystem system, IEntity entity, bool showUid, bool isRoot) : base(system, showUid, isRoot)
            {
                _entity = entity;

                var text = Loc.GetString(ShowUid ? $"{entity.Name} ({entity.Uid})" : entity.Name);
                Margin.AddChild(new Label { Text = text });

                var control = new HBoxContainer
                {
                    SeparationOverride = 6,
                    Children =
                    {
                        new LayoutContainer
                        {
                            Children =
                            {
                                new SpriteView { Sprite = _entity.GetComponent<ISpriteComponent>() },
                            }
                        },
                        Margin
                    }
                };
                AddChild(control);

                if (_entity.TryGetComponent(out _sprite))
                {
                    _oldDrawDepth = _sprite.DrawDepth;
                }
                _outline = _entity.GetComponentOrNull<InteractionOutlineComponent>();
            }

            protected override void MouseEntered()
            {
                base.MouseEntered();

                if (VSystem._currentGroupList != null)
                {
                    VSystem.CloseGroupMenu();
                }

                if (IsRoot && VSystem._contextPopupExt != null)
                {
                    VSystem.CloseContextPopupExt();
                }

                if (_entity != null && !_entity.Deleted)
                {
                    var localPlayer = VSystem._playerManager.LocalPlayer;
                    if (localPlayer != null && localPlayer.ControlledEntity != null)
                    {
                        _sprite.DrawDepth = (int) Shared.GameObjects.DrawDepth.HighlightedItems;
                        _outline?.OnMouseEnter(localPlayer.InRangeUnobstructed(_entity, ignoreInsideBlocker: true));
                    }
                }
                _drawOutline = true;
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);
                if (_entity != null && !_entity.Deleted && _drawOutline)
                {
                    var localPlayer = VSystem._playerManager.LocalPlayer;
                    if (localPlayer != null && localPlayer.ControlledEntity != null)
                    {
                        _outline?.OnMouseEnter(localPlayer.InRangeUnobstructed(_entity, ignoreInsideBlocker: true));
                    }
                }
            }

            protected override void MouseExited()
            {
                base.MouseExited();
                if (_entity != null && !_entity.Deleted)
                {
                    _sprite.DrawDepth = _oldDrawDepth;
                    _outline?.OnMouseLeave();
                }
                _drawOutline = false;
            }

            protected override void KeyBindDown(GUIBoundKeyEventArgs args)
            {
                base.KeyBindDown(args);
                ContextKeyBindDown(args, _entity);
            }
        }

        private sealed class MultiContextElement : ContextElement
        {
            private readonly List<IEntity> _entities;
            private CancellationTokenSource _openCancel;
            private static readonly TimeSpan HoverDelay = TimeSpan.FromSeconds(0.2);

            public MultiContextElement(VerbSystem system, IEnumerable<IEntity> entities, bool showUid) : base(system, showUid, true)
            {
                _entities = new(entities);

                var entity = _entities.First();

                var labelCount = new Label
                {
                    Text = Loc.GetString(_entities.Count.ToString()),
                    StyleClasses = { StyleNano.StyleClassContextMenuCount },
                };
                LayoutContainer.SetAnchorPreset(labelCount, LayoutContainer.LayoutPreset.BottomRight);
                LayoutContainer.SetGrowHorizontal(labelCount, LayoutContainer.GrowDirection.Begin);
                LayoutContainer.SetGrowVertical(labelCount, LayoutContainer.GrowDirection.Begin);

                var text = showUid ? $"{entity.Name} (---)" : entity.Name;
                Margin.AddChild(new Label { Text = Loc.GetString(text) } );

                var control = new HBoxContainer
                {
                    SeparationOverride = 6,
                    Children =
                    {
                        new LayoutContainer
                        {
                            Children =
                            {
                                new SpriteView { Sprite = entity.GetComponent<ISpriteComponent>() },
                                labelCount
                            }
                        },
                        Margin,
                        new TextureRect
                        {
                            Texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Interface/VerbIcons/group.svg.96dpi.png"),
                            Stretch = TextureRect.StretchMode.KeepCentered,
                        }
                    }
                };
                AddChild(control);
            }

            protected override void KeyBindDown(GUIBoundKeyEventArgs args)
            {
                base.KeyBindDown(args);
                // TODO: Get the next entity if the first one is unavailable (deleted / taken / out of range).
                // TODO: Edit similar entities at the same time ?
                ContextKeyBindDown(args, _entities.First(), false);
            }

            protected override void MouseEntered()
            {
                base.MouseEntered();

                _openCancel = new CancellationTokenSource();

                Timer.Spawn(HoverDelay, () =>
                {
                    if (VSystem._currentGroupList != null)
                    {
                        VSystem.CloseGroupMenu();
                    }

                    if (VSystem._contextPopupExt != null)
                    {
                        VSystem.CloseContextPopupExt();
                    }

                    VSystem._contextPopupExt = new ContextPopup();
                    foreach (var entity in _entities)
                    {
                        if (!entity.Deleted)
                        {
                            VSystem._contextPopupExt.AddElement(new SingleContextElement(VSystem, entity, ShowUid, false), true);
                        }
                    }

                    UserInterfaceManager.ModalRoot.AddChild(VSystem._contextPopupExt);

                    var size = VSystem._contextPopupExt.List.CombinedMinimumSize;
                    VSystem._contextPopupExt.Open(UIBox2.FromDimensions(GlobalPosition + (Width, 0), size));
                }, _openCancel.Token);
            }

            protected override void MouseExited()
            {
                base.MouseExited();

                _openCancel?.Cancel();
                _openCancel = null;
            }
        }

        private sealed class ContextMenuPopup : Popup
        {
            private const int MaxItemBeforeScroll = 10;

            public Dictionary<EntityUid, Control> realChildren;
            public VBoxContainer List { get; }

            public ContextMenuPopup()
            {
                realChildren = new();
                AddChild(new ScrollContainer
                {
                    HScrollEnabled = false,
                    Children = { new PanelContainer
                    {
                        Children = { (List = new VBoxContainer()) },
                        PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#111E") }
                    }}
                });
            }

            public void AddE(IEnumerable<IEntity> entities, VerbSystem verbSystem, bool isDebug)
            {
                if (entities.Count() > 1)
                {
                    AddMultiple(entities);
                }
                else
                {
                    var single = new SingleContextElement(verbSystem, entities.First(), isDebug, )
                    AddSingle(entities.First());

                }
                // List.AddChild(e);
                // if (addSeparation)
                // {
                //     AddSeparation();
                // }
                // _contextElementCount++;
            }

            private void AddSingle(IEntity entity)
            {
                var single = new SingleContextElement()
                {

                };
            }

            private void AddMultiple(IEnumerable<IEntity> entities)
            {

            }

        }

        private sealed class ContextPopup : Popup
        {
            private const int MaxItemsBeforeScroll = 10;
            private int _contextElementCount = 0;

            public VBoxContainer List { get; }

            public Dictionary<EntityUid, Control> hm = new();

            public ContextPopup()
            {
                AddChild(new ScrollContainer
                {
                    HScrollEnabled = false,
                    Children = { new PanelContainer
                    {
                        Children = { (List = new VBoxContainer()) },
                        PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#111E") }
                    }}
                });
            }

            public void AddElement(IEnumerable<IEntity> entities)
            {

            }

            private void AddSeparation()
            {
                List.AddChild(new PanelContainer
                {
                    CustomMinimumSize = (0, 2),
                    PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#333") }
                });
            }


            public void ASingle(VerbSystem vs, IEntity ls, bool debug, bool isRoot)
            {
                var a = new SingleContextElement(vs, ls, debug, isRoot);
                var b = new PanelContainer()
                {
                    CustomMinimumSize = (0, 2),
                    PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#333")}
                };
                var f = new VBoxContainer()
                {
                    Children = {a, b}
                };
                List.AddChild(f);
                _contextElementCount++;
            }
            public void AMulti(VerbSystem vs, IEnumerable<IEntity> ls, bool debug)
            {
                var a = new MultiContextElement(vs, ls, debug);
                var b = new PanelContainer()
                {
                    CustomMinimumSize = (0, 2),
                    PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#333")}
                };
                var f = new VBoxContainer()
                {
                    Children = {a, b}
                };
                List.AddChild(f);
                _contextElementCount++;
            }

            public void AddElement(SingleContextElement e, bool addSeparation)
            {
                List.AddChild(e);
                if (addSeparation)
                {
                    AddSeparation();
                }
                _contextElementCount++;
            }

            public void AddElement(MultiContextElement e, bool addSeparation)
            {
                List.AddChild(e);
                if (addSeparation)
                {
                    AddSeparation();
                }
                _contextElementCount++;
            }

            protected override Vector2 CalculateMinimumSize()
            {
                var size = base.CalculateMinimumSize();
                size.Y = _contextElementCount > MaxItemsBeforeScroll ? MaxItemsBeforeScroll * 32 + MaxItemsBeforeScroll * 2 : size.Y;
                return size;
            }
        }
    }
}

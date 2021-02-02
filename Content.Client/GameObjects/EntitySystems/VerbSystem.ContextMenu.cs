using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Content.Client.GameObjects.Components;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.Input;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
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
            protected readonly bool IsDebug;
            protected readonly int Depth;
            public ContextMenuElement(int depth, VerbSystem verbSystem, bool isDebug)
            {
                Depth = depth;
                VSystem = verbSystem;
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

            protected abstract void InitializeContextMenuElement();
        }

        private sealed class SingleContextElement : ContextMenuElement
        {
            private IEntity _entity;
            private ISpriteComponent _sprite;

            private InteractionOutlineComponent _outline;
            private bool _drawOutline;
            private int _oldDrawDepth;

            public SingleContextElement(IEntity entity, int depth, VerbSystem verbSystem, bool isDebug) : base(depth, verbSystem, isDebug)
            {
                _entity = entity;

                if (_entity.TryGetComponent(out _sprite))
                {
                    _oldDrawDepth = _sprite.DrawDepth;
                }

                _outline = _entity.GetComponentOrNull<InteractionOutlineComponent>();

                InitializeContextMenuElement();
            }

            protected override void KeyBindDown(GUIBoundKeyEventArgs args)
            {
                if (args.Function == ContentKeyFunctions.OpenContextMenu)
                {
                    VSystem.OnContextButtonPressed(_entity);
                    return;
                }

                if (args.Function == ContentKeyFunctions.ExamineEntity)
                {
                    Get<ExamineSystem>().DoExamine(_entity);
                    return;
                }

                if (args.Function == EngineKeyFunctions.Use || args.Function == ContentKeyFunctions.Point ||
                    args.Function == ContentKeyFunctions.TryPullObject || args.Function == ContentKeyFunctions.MovePulledObject)
                {
                    var inputSys = VSystem.EntitySystemManager.GetEntitySystem<InputSystem>();

                    var func = args.Function;
                    var funcId = VSystem._inputManager.NetworkBindMap.KeyFunctionID(func);

                    var message = new FullInputCmdMessage(VSystem._gameTiming.CurTick, VSystem._gameTiming.TickFraction, funcId,
                        BoundKeyState.Down, _entity.Transform.Coordinates, args.PointerLocation, _entity.Uid);

                    var session = VSystem._playerManager.LocalPlayer.Session;
                    inputSys.HandleInputCommand(session, func, message);

                    VSystem.CloseAllMenus();
                    return;
                }

                if (VSystem._itemSlotManager.OnButtonPressed(args, _entity))
                {
                    VSystem.CloseAllMenus();
                }
            }

            protected override void MouseEntered()
            {
                base.MouseEntered();

                if (VSystem._currentGroupList != null)
                {
                    VSystem.CloseGroupMenu();
                }

                // if (VSystem._contextPopupExt != null)
                // {
                    // VSystem.CloseContextPopupExt();
                // }
                while (VSystem.StackContextMenus.Peek().Depth > Depth)
                {
                    VSystem.StackContextMenus.Pop()?.Dispose();;
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

            private void RestoreSprite()
            {
                if (_entity != null && !_entity.Deleted)
                {
                    _sprite.DrawDepth = _oldDrawDepth;
                    _outline?.OnMouseLeave();
                }
                _drawOutline = false;
            }

            protected override void MouseExited()
            {
                base.MouseExited();
                RestoreSprite();
            }

            protected override void Dispose(bool disposing)
            {
                RestoreSprite();
                base.Dispose(disposing);
            }

            protected override void InitializeContextMenuElement()
            {
                AddChild(
                    new HBoxContainer
                    {
                        SeparationOverride = 6,
                        Children =
                        {
                            new LayoutContainer
                            {
                                Children = { new SpriteView { Sprite = _entity.GetComponent<ISpriteComponent>() } }
                            },
                            new MarginContainer
                            {
                                MarginLeftOverride = 4, MarginRightOverride = 4,
                                Children = { new Label {Text = Loc.GetString(IsDebug ? $"{_entity.Name} ({_entity.Uid})" : _entity.Name) } }
                            },
                        }
                    }
                );
            }
        }

        private sealed class StackContextElement : ContextMenuElement
        {
            private static readonly TimeSpan HoverDelay = TimeSpan.FromSeconds(0.2);

            private HashSet<IEntity> _entities;

            private ISpriteComponent _sprite;
            private Label _label;
            public StackContextElement(IEnumerable<IEntity> entities, int depth, VerbSystem verbSystem, bool isDebug) : base(depth, verbSystem, isDebug)
            {
                _entities = new(entities);
                _sprite = _entities.First().GetComponent<ISpriteComponent>();
                _label = new Label
                {
                    Text = Loc.GetString(_entities.Count.ToString()),
                    StyleClasses = { StyleNano.StyleClassContextMenuCount },
                };
                InitializeContextMenuElement();
            }

            protected override void InitializeContextMenuElement()
            {
                LayoutContainer.SetAnchorPreset(_label, LayoutContainer.LayoutPreset.BottomRight);
                LayoutContainer.SetGrowHorizontal(_label, LayoutContainer.GrowDirection.Begin);
                LayoutContainer.SetGrowVertical(_label, LayoutContainer.GrowDirection.Begin);

                AddChild(
                    new HBoxContainer()
                    {
                        SeparationOverride = 6,
                        Children =
                        {
                            new LayoutContainer
                            {
                                Children = { new SpriteView { Sprite = _sprite }, _label }
                            },
                            new MarginContainer
                            {
                                MarginLeftOverride = 4, MarginRightOverride = 4,
                                Children = { new Label { Text = Loc.GetString(_entities.First().Name) } }
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

                Timer.Spawn(HoverDelay, () =>
                {
                    if (VSystem._currentGroupList != null)
                    {
                        VSystem.CloseGroupMenu();
                    }

                    // if (VSystem._contextPopupExt != null)
                    // {
                    // VSystem.CloseContextPopupExt();
                    // }
                    while (VSystem.StackContextMenus.Peek().Depth > Depth)
                    {
                        VSystem.StackContextMenus.Pop()?.Dispose();;
                    }

                    var filteredEntities = _entities.Where(entity => !entity.Deleted);
                    if (filteredEntities.Any())
                    {
                        // VSystem._contextPopupExt = new ContextMenuPopup();
                        var lastDepth = VSystem.StackContextMenus.Peek().Depth;
                        var _contextPopupExt = new ContextMenuPopup(lastDepth + 1);
                        foreach (var entity in filteredEntities)
                        {
                            _contextPopupExt.AddContextElement(entity, VSystem, IsDebug);
                        }
                        ///Remove after :
                        _contextPopupExt.AddContextElement(filteredEntities, VSystem, IsDebug);
                        ///

                        VSystem.StackContextMenus.Push(_contextPopupExt);
                        UserInterfaceManager.ModalRoot.AddChild(_contextPopupExt);

                        var size = _contextPopupExt.List.CombinedMinimumSize;
                        _contextPopupExt.Open(UIBox2.FromDimensions(GlobalPosition + (Width, 0), size));
                    }
                }, new CancellationTokenSource().Token);
            }
        }

        private sealed class ContextMenuPopup : Popup
        {
            private const int MaxItemsBeforeScroll = 10;
            private static readonly Color DefaultColor = Color.FromHex("#111E");
            private static readonly Color HighlightedColor = Color.FromHex("#111E");
            private static readonly Color SeparationColor = Color.FromHex("#333");

            public int Depth { get; }

            public  VBoxContainer List { get; }
            public readonly Dictionary<ContextMenuElement, Control> Elements = new();

            public ContextMenuPopup(int depth = 0)
            {
                Depth = depth;
                AddChild(new ScrollContainer
                {
                    HScrollEnabled = false,
                    Children = { new PanelContainer
                    {
                        Children = { (List = new VBoxContainer()) },
                        PanelOverride = new StyleBoxFlat { BackgroundColor = DefaultColor }
                    }}
                });
            }

            private void Add(ContextMenuElement element, Control control)
            {
                List.AddChild(control);
                Elements.Add(element, control);
            }

            private void Remove(ContextMenuElement element)
            {
                var control = Elements[element];
                List.RemoveChild(control);
                Elements.Remove(element);
            }

            public void AddContextElement(IEntity entity, VerbSystem verbSystem, bool isDebug)
            {
                var element = new SingleContextElement(entity, Depth, verbSystem, isDebug);
                Add(element, new VBoxContainer { Children = { element, Separation() } });
            }

            public void AddContextElement(IEnumerable<IEntity> entities, VerbSystem verbSystem, bool isDebug)
            {
                if (entities.Count() > 1)
                {
                    var element = new StackContextElement(entities, Depth, verbSystem, isDebug);
                    Add(element, new VBoxContainer { Children = { element, Separation() } });
                }
                else
                {
                    AddContextElement(entities.First(), verbSystem, isDebug);
                }
            }

            protected override Vector2 CalculateMinimumSize()
            {
                var size = base.CalculateMinimumSize();
                size.Y = Elements.Count > MaxItemsBeforeScroll ? MaxItemsBeforeScroll * 32 + MaxItemsBeforeScroll * 2 : size.Y;
                return Vector2.ComponentMin(size, List.CombinedMinimumSize);
            }

            private static Control Separation()
            {
                return new PanelContainer
                {
                    CustomMinimumSize = (0, 2),
                    PanelOverride = new StyleBoxFlat { BackgroundColor = SeparationColor }
                };
            }
        }
    }
}

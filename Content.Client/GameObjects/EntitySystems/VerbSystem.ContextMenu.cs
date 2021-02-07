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
        private static void Propagate(IEntity entity, StackContextElement stack)
        {
            while (stack != null)
            {
                stack.RemoveOneEntity(entity);
                if (stack.EntitiesCount == 0)
                {
                    var menu = stack.ParentMenu;
                    menu.Remove(stack);
                }
                stack = stack.Pre;
            }
        }
        private static Control Separation()
        {
            return new PanelContainer
            {
                CustomMinimumSize = (0, 2),
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#333") }
            };
        }
        private static List<List<IEntity>> GroupEntities(IEnumerable<IEntity> entities, int depth = 0)
        {
            if (GroupingContextMenuType == 0)
            {
                var newEntities = entities.GroupBy(e => e, new PrototypeAndStatesContextMenuComparer(depth)).ToList();
                while (newEntities.Count == 1 && depth++ < PrototypeAndStatesContextMenuComparer.Count)
                {
                    newEntities = entities.GroupBy(e => e, new PrototypeAndStatesContextMenuComparer(depth)).ToList();
                }
                return newEntities.Select(grp => grp.ToList()).ToList();
            }
            else
            {
                var newEntities = entities.GroupBy(e => e, new PrototypeContextMenuComparer()).ToList();
                return newEntities.Select(grp => grp.ToList()).ToList();
            }
        }

        private abstract class ContextMenuElement : Control
        {
            private static readonly Color HoverColor = Color.DarkSlateGray;

            protected readonly VerbSystem VSystem;
            protected readonly bool IsDebug;
            protected readonly int Depth;

            protected internal readonly ContextMenuPopup ParentMenu;

            protected ContextMenuElement(int depth, VerbSystem verbSystem, bool isDebug, ContextMenuPopup parentMenu)
            {
                Depth = depth;
                VSystem = verbSystem;
                IsDebug = isDebug;

                ParentMenu = parentMenu;

                MouseFilter = MouseFilterMode.Stop;
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);

                if (UserInterfaceManager.CurrentlyHovered == this)
                {
                    handle.DrawRect(PixelSizeBox, HoverColor);
                }
            }
        }

        private sealed class SingleContextElement : ContextMenuElement
        {
            private IEntity ContextEntity{ get; }
            public readonly StackContextElement Pre;

            private readonly ISpriteComponent _sprite;
            private readonly InteractionOutlineComponent _outline;
            private readonly int _oldDrawDepth;
            private bool _drawOutline;

            public SingleContextElement(IEntity entity, StackContextElement pre, int depth, VerbSystem verbSystem, bool isDebug, ContextMenuPopup parentMenu)
                : base(depth, verbSystem, isDebug, parentMenu)
            {
                Pre = pre;
                ContextEntity = entity;
                if (ContextEntity.TryGetComponent(out _sprite))
                {
                    _oldDrawDepth = _sprite.DrawDepth;
                }

                _outline = ContextEntity.GetComponentOrNull<InteractionOutlineComponent>();
                InitializeContextMenuElement();
            }

            protected override void KeyBindDown(GUIBoundKeyEventArgs args)
            {
                if (args.Function == ContentKeyFunctions.OpenContextMenu)
                {
                    VSystem.OnContextButtonPressed(ContextEntity);
                    return;
                }

                if (args.Function == ContentKeyFunctions.ExamineEntity)
                {
                    Get<ExamineSystem>().DoExamine(ContextEntity);
                    return;
                }

                if (args.Function == EngineKeyFunctions.Use || args.Function == ContentKeyFunctions.Point ||
                    args.Function == ContentKeyFunctions.TryPullObject || args.Function == ContentKeyFunctions.MovePulledObject)
                {
                    var inputSys = VSystem.EntitySystemManager.GetEntitySystem<InputSystem>();

                    var func = args.Function;
                    var funcId = VSystem._inputManager.NetworkBindMap.KeyFunctionID(func);

                    var message = new FullInputCmdMessage(VSystem._gameTiming.CurTick, VSystem._gameTiming.TickFraction, funcId,
                        BoundKeyState.Down, ContextEntity.Transform.Coordinates, args.PointerLocation, ContextEntity.Uid);

                    var session = VSystem._playerManager.LocalPlayer.Session;
                    inputSys.HandleInputCommand(session, func, message);

                    VSystem.CloseAllMenus();
                    return;
                }

                if (VSystem._itemSlotManager.OnButtonPressed(args, ContextEntity))
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

                VSystem.CloseContextPopups(Depth);

                if (ContextEntity.Deleted) return;

                var localPlayer = VSystem._playerManager.LocalPlayer;
                if (localPlayer?.ControlledEntity != null)
                {
                    _outline?.OnMouseEnter(localPlayer.InRangeUnobstructed(ContextEntity, ignoreInsideBlocker: true));
                    _sprite.DrawDepth = (int) Shared.GameObjects.DrawDepth.HighlightedItems;
                    _drawOutline = true;
                }
            }

            protected override void MouseExited()
            {
                base.MouseExited();
                if (!ContextEntity.Deleted)
                {
                    _sprite.DrawDepth = _oldDrawDepth;
                    _outline?.OnMouseLeave();
                }
                _drawOutline = false;
            }

            protected override void Dispose(bool disposing)
            {
                var parent = Pre;
                if (parent != null)
                {
                    VSystem._entityMenuElements[ContextEntity] = parent;
                }
                base.Dispose(disposing);
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);
                if (!_drawOutline) return;

                var localPlayer = VSystem._playerManager.LocalPlayer;
                if (localPlayer?.ControlledEntity != null)
                {
                    _outline?.UpdateInRange(localPlayer.InRangeUnobstructed(ContextEntity, ignoreInsideBlocker: true));
                }
            }

            private void InitializeContextMenuElement()
            {
                AddChild(
                    new HBoxContainer
                    {
                        SeparationOverride = 6,
                        Children =
                        {
                            new LayoutContainer
                            {
                                Children = { new SpriteView { Sprite = ContextEntity.GetComponent<ISpriteComponent>() } }
                            },
                            new MarginContainer
                            {
                                MarginLeftOverride = 4, MarginRightOverride = 4,
                                Children = { new Label
                                {
                                    Text = Loc.GetString(IsDebug ? $"{ContextEntity.Name} ({ContextEntity.Uid})" : ContextEntity.Name)
                                } }
                            },
                        }
                    }
                );
            }
        }

        private sealed class StackContextElement : ContextMenuElement
        {
            private static readonly TimeSpan HoverDelay = TimeSpan.FromSeconds(0.2);

            private HashSet<IEntity> ContextEntities { get; }
            public readonly StackContextElement Pre;

            private readonly SpriteView _spriteView;
            private readonly Label _label;

            public int EntitiesCount => ContextEntities.Count;

            public StackContextElement(IEnumerable<IEntity> entities, StackContextElement pre, int depth, VerbSystem verbSystem, bool isDebug, ContextMenuPopup parentMenu)
                : base(depth, verbSystem, isDebug, parentMenu)
            {
                Pre = pre;
                ContextEntities = new(entities);
                _spriteView = new SpriteView()
                {
                    Sprite = ContextEntities.First().GetComponent<ISpriteComponent>()
                };
                _label = new Label
                {
                    Text = Loc.GetString(ContextEntities.Count.ToString()),
                    StyleClasses = { StyleNano.StyleClassContextMenuCount },
                };
                InitializeContextMenuElement();
            }

            protected override void KeyBindDown(GUIBoundKeyEventArgs args)
            {
                var firstEntity = ContextEntities.FirstOrDefault(e => !e.Deleted);

                if (firstEntity == null) return;

                if (args.Function == EngineKeyFunctions.Use || args.Function == ContentKeyFunctions.TryPullObject || args.Function == ContentKeyFunctions.MovePulledObject)
                {
                    var inputSys = VSystem.EntitySystemManager.GetEntitySystem<InputSystem>();

                    var func = args.Function;
                    var funcId = VSystem._inputManager.NetworkBindMap.KeyFunctionID(func);

                    var message = new FullInputCmdMessage(VSystem._gameTiming.CurTick, VSystem._gameTiming.TickFraction, funcId,
                        BoundKeyState.Down, firstEntity.Transform.Coordinates, args.PointerLocation, firstEntity.Uid);

                    var session = VSystem._playerManager.LocalPlayer.Session;
                    inputSys.HandleInputCommand(session, func, message);

                    VSystem.CloseAllMenus();
                    return;
                }

                if (VSystem._itemSlotManager.OnButtonPressed(args, firstEntity))
                {
                    VSystem.CloseAllMenus();
                }
            }

            public void RemoveOneEntity(IEntity entity)
            {
                ContextEntities.Remove(entity);

                _label.Text = Loc.GetString(ContextEntities.Count.ToString());
                _spriteView.Sprite = ContextEntities.FirstOrDefault(e => !e.Deleted)?.GetComponent<ISpriteComponent>();
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

                    if (VSystem._stackContextMenus.Count == 0)
                    {
                        return;
                    }

                    VSystem.CloseContextPopups(Depth);

                    var filteredEntities = ContextEntities.Where(entity => !entity.Deleted);
                    if (!filteredEntities.Any()) return;

                    var newContextMenu = new ContextMenuPopup(VSystem._stackContextMenus.Peek().Depth + 1);
                    newContextMenu.FillContextMenuPopup(GroupEntities(filteredEntities, Depth + 1), this, VSystem, IsDebug);

                    VSystem._stackContextMenus.Push(newContextMenu);
                    UserInterfaceManager.ModalRoot.AddChild(newContextMenu);

                    var size = newContextMenu.List.CombinedMinimumSize;
                    newContextMenu.Open(UIBox2.FromDimensions(GlobalPosition + (Width, 0), size));
                }, new CancellationTokenSource().Token);
            }

            protected override void Dispose(bool disposing)
            {
                if (Pre != null)
                {
                    foreach (var entity in ContextEntities)
                    {
                        VSystem._entityMenuElements[entity] = Pre;
                    }
                }

                base.Dispose(disposing);
            }

            private void InitializeContextMenuElement()
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
                            new LayoutContainer { Children = { _spriteView, _label } },
                            new MarginContainer
                            {
                                MarginLeftOverride = 4, MarginRightOverride = 4,
                                Children = { new Label { Text = Loc.GetString(ContextEntities.First().Name) } }
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
        }

        private sealed class ContextMenuPopup : Popup
        {
            private const int MaxItemsBeforeScroll = 10;
            private static readonly Color DefaultColor = Color.FromHex("#111E");

            public int Depth { get; }
            public  VBoxContainer List { get; }
            private readonly Dictionary<ContextMenuElement, Control> _controls = new();

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

            public void RemoveEntityFrom(ContextMenuElement element, IEntity entity)
            {
                switch (element)
                {
                    case SingleContextElement singleContextElement:
                        Remove(singleContextElement);
                        Propagate(entity, singleContextElement.Pre);
                        break;
                    case StackContextElement stackContextElement:
                        stackContextElement.RemoveOneEntity(entity);
                        if (stackContextElement.EntitiesCount == 0)
                        {
                            Remove(stackContextElement);
                        }
                        Propagate(entity, stackContextElement.Pre);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(element));
                }
            }

            public void Remove(ContextMenuElement element)
            {
                List.RemoveChild(_controls[element]);
                _controls.Remove(element);

                if (_controls.Count == 0)
                {
                    RemoveAllChildren();
                }
            }

            private void Add(ContextMenuElement element, Control control)
            {
                List.AddChild(control);
                _controls.Add(element, control);
            }

            private void AddSingleContextElement(IEntity entity, StackContextElement pre, VerbSystem verbSystem, bool isDebug)
            {
                var element = new SingleContextElement(entity, pre, Depth, verbSystem, isDebug, this);
                Add(element, new VBoxContainer { Children = { element, Separation() } });

                verbSystem.AddToUI(entity, element);
            }

            private void AddStackContextElement(IEnumerable<IEntity> entities, StackContextElement pre,
                VerbSystem verbSystem, bool isDebug)
            {
                var element = new StackContextElement(entities, pre, Depth, verbSystem, isDebug, this);
                Add(element, new VBoxContainer {Children = {element, Separation()}});

                foreach (var entity in entities)
                {
                    verbSystem.AddToUI(entity, element);
                }
            }

            public void FillContextMenuPopup(List<List<IEntity>> entities, StackContextElement pre, VerbSystem verbSystem, bool isDebug)
            {
                if (entities.Count == 1)
                {
                    foreach (var entity in entities[0])
                    {
                        AddSingleContextElement(entity, pre, verbSystem, isDebug);
                    }
                }
                else
                {
                    foreach (var entity in entities)
                    {
                        if (entity.Count == 1)
                        {
                            AddSingleContextElement(entity[0], pre, verbSystem, isDebug);
                        }
                        else
                        {
                            AddStackContextElement(entity, pre, verbSystem, isDebug);
                        }
                    }
                }
            }

            protected override Vector2 CalculateMinimumSize()
            {
                var size = base.CalculateMinimumSize();
                size.Y = _controls.Count > MaxItemsBeforeScroll ? MaxItemsBeforeScroll * 32 + MaxItemsBeforeScroll * 2 : size.Y;
                return Vector2.ComponentMin(size, List.CombinedMinimumSize);
            }
        }
    }
}

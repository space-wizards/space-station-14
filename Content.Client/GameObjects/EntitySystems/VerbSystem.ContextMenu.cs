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
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Client.GameObjects.EntitySystems
{
    public sealed partial class VerbSystem
    {
        private abstract class ContextMenuElement : Control
        {
            private static readonly Color HoverColor = Color.DarkSlateGray;

            public readonly ContextMenuElement Pre;

            protected readonly VerbSystem VSystem;
            protected readonly bool IsDebug;
            protected readonly int Depth;

            protected ContextMenuElement(int depth, VerbSystem verbSystem, bool isDebug, ContextMenuElement pre = null)
            {
                Depth = depth;
                VSystem = verbSystem;
                IsDebug = isDebug;

                Pre = pre;

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

            protected abstract void InitializeContextMenuElement();
        }

        private sealed class SingleContextElement : ContextMenuElement
        {
            private ShaderInstance _dropTargetInRangeShader;
            private ShaderInstance _dropTargetOutOfRangeShader;
            public IEntity ContextEntity{ get; }

            private ISpriteComponent _sprite;
            private InteractionOutlineComponent _outline;
            private bool _drawOutline;
            private int _oldDrawDepth;

            public SingleContextElement(IEntity entity, ContextMenuElement pre, int depth, VerbSystem verbSystem, bool isDebug) : base(depth, verbSystem, isDebug, pre)
            {
                ContextEntity = entity;
                if (ContextEntity.TryGetComponent(out _sprite))
                {
                    _oldDrawDepth = _sprite.DrawDepth;
                }

                _outline = ContextEntity.GetComponentOrNull<InteractionOutlineComponent>();
                if (_outline != null)
                {
                    _dropTargetInRangeShader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("SelectionOutlineInrange").Instance();
                    _dropTargetOutOfRangeShader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("SelectionOutline").Instance();
                }

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

                if (!ContextEntity.Deleted)
                {
                    var localPlayer = VSystem._playerManager.LocalPlayer;
                    if (localPlayer?.ControlledEntity != null)
                    {
                        _outline?.OnMouseEnter(localPlayer.InRangeUnobstructed(ContextEntity, ignoreInsideBlocker: true));
                        _sprite.DrawDepth = (int) Shared.GameObjects.DrawDepth.HighlightedItems;
                        _drawOutline = true;
                    }
                }
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);
                if (_drawOutline)
                {
                    var localPlayer = VSystem._playerManager.LocalPlayer;
                    if (localPlayer?.ControlledEntity != null)
                    {
                        _outline?.UpdateInRange(localPlayer.InRangeUnobstructed(ContextEntity, ignoreInsideBlocker: true));
                    }
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

            public HashSet<IEntity> ContextEntities { get; }
            private SpriteView _spriteView;
            private Label _label;

            public StackContextElement(IEnumerable<IEntity> entities, ContextMenuElement pre, int depth, VerbSystem verbSystem, bool isDebug) : base(depth, verbSystem, isDebug, pre)
            {
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

            public void UpdateLabelAndSprite()
            {
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

                    VSystem.CloseContextPopups(Depth);

                    if (VSystem.StackContextMenus.Count == 0)
                    {
                        return;
                    }

                    var filteredEntities = ContextEntities.Where(entity => !entity.Deleted);
                    if (filteredEntities.Any())
                    {
                        var newContextMenu = new ContextMenuPopup(VSystem.StackContextMenus.Peek().Depth + 1);
                        foreach (var entity in filteredEntities)
                        {
                            VSystem.menus.Remove(entity);
                            newContextMenu.AddContextElement(entity, this, VSystem, IsDebug);
                        }

                        VSystem.StackContextMenus.Push(newContextMenu);
                        UserInterfaceManager.ModalRoot.AddChild(newContextMenu);

                        var size = newContextMenu.List.CombinedMinimumSize;
                        newContextMenu.Open(UIBox2.FromDimensions(GlobalPosition + (Width, 0), size));
                    }
                }, new CancellationTokenSource().Token);
            }
        }

        private sealed class ContextMenuPopup : Popup
        {
            private const int MaxItemsBeforeScroll = 10;
            private static readonly Color DefaultColor = Color.FromHex("#111E");
            private static readonly Color SeparationColor = Color.FromHex("#333");

            public int Depth { get; }
            public  VBoxContainer List { get; }
            public  ScrollContainer ScrollList { get; }
            public readonly Dictionary<ContextMenuElement, Control> Elements = new();

            public ContextMenuPopup(int depth = 0)
            {
                Depth = depth;
                AddChild(ScrollList = new ScrollContainer
                {
                    HScrollEnabled = false,
                    Children = { new PanelContainer
                    {
                        Children = { (List = new VBoxContainer()) },
                        PanelOverride = new StyleBoxFlat { BackgroundColor = DefaultColor }
                    }}
                });
            }

            protected override void FrameUpdate(FrameEventArgs args)
            {
                base.FrameUpdate(args);

                foreach (var (k, _) in Elements)
                {
                    if (k is SingleContextElement single && (single.ContextEntity.Deleted || single.ContextEntity.IsInContainer()))
                    {
                        Remove(single);
                    }
                    if (k is StackContextElement stack && stack.ContextEntities.Count == 0)
                    {
                        List.RemoveChild(Elements[stack]);
                        Elements.Remove(stack);
                    }
                }
                if (Elements.Count == 0)
                {
                    RemoveAllChildren();
                }
            }

            private float maxX = 0.0f;
            private void Add(ContextMenuElement element, Control control)
            {
                List.AddChild(control);
                Elements.Add(element, control);

                maxX = Math.Max(ScrollList.CombinedMinimumSize.X, maxX);
            }

            public void Remove(SingleContextElement element)
            {
                List.RemoveChild(Elements[element]);
                Elements.Remove(element);

                if (element.Pre is StackContextElement parent)
                {
                    parent.ContextEntities.Remove(element.ContextEntity);
                    if (parent.ContextEntities.Count > 0)
                    {
                        parent.UpdateLabelAndSprite();
                    }
                }
            }

            public void AddContextElement(IEntity entity, ContextMenuElement pre, VerbSystem verbSystem, bool isDebug)
            {
                var element = new SingleContextElement(entity, pre, Depth, verbSystem, isDebug);
                Add(element, new VBoxContainer { Children = { element, Separation() } });
                verbSystem.menus.Add(entity, (this, element));
            }

            public void AddContextElement(IEnumerable<IEntity> entities, ContextMenuElement pre, VerbSystem verbSystem, bool isDebug)
            {
                if (entities.Count() > 1)
                {
                    var element = new StackContextElement(entities, pre, Depth, verbSystem, isDebug);
                    Add(element, new VBoxContainer { Children = { element, Separation() } });
                }
                else
                {
                    AddContextElement(entities.First(), pre, verbSystem, isDebug);
                }
            }

            protected override Vector2 CalculateMinimumSize()
            {
                var size = base.CalculateMinimumSize();
                size.Y = Elements.Count > MaxItemsBeforeScroll ? MaxItemsBeforeScroll * 32 + MaxItemsBeforeScroll * 2 : size.Y;
                var f = Vector2.ComponentMin(size, List.CombinedMinimumSize);
                f.X = maxX;
                return f;
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

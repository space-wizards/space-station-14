#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.ContextMenu
{
    public interface IContextMenuView : IDisposable
    {
        Dictionary<IEntity, ContextMenuElement> Elements { get; set; }
        Stack<ContextMenuPopup> Menus { get; }
        event EventHandler<(GUIBoundKeyEventArgs, SingleContextElement)>? OnKeyBindDownSingle;
        event EventHandler<SingleContextElement>? OnMouseEnteredSingle;
        event EventHandler<SingleContextElement>? OnMouseExitedSingle;
        event EventHandler<SingleContextElement>? OnMouseHoveringSingle;

        event EventHandler<(GUIBoundKeyEventArgs, StackContextElement)>? OnKeyBindDownStack;
        event EventHandler<StackContextElement>? OnMouseEnteredStack;

        event EventHandler<ContextMenuElement>? OnExitedTree;

        event EventHandler? OnCloseRootMenu;
        event EventHandler<int>? OnCloseChildMenu;

        void UpdateParents(ContextMenuElement element);
        void RemoveEntity(IEntity element);
        void AddRootMenu(List<IEntity> entities);
        void AddChildMenu(IEnumerable<IEntity> entities, Vector2 position, StackContextElement? stack);
        void CloseContextPopups(int depth);
        void CloseContextPopups();

        void OnGroupingContextMenuChanged(int obj);
    }

    public partial class ContextMenuView : IContextMenuView
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

        public Stack<ContextMenuPopup> Menus { get; }
        public Dictionary<IEntity, ContextMenuElement> Elements { get; set; }

        public event EventHandler<(GUIBoundKeyEventArgs, SingleContextElement)>? OnKeyBindDownSingle;
        public event EventHandler<SingleContextElement>? OnMouseEnteredSingle;
        public event EventHandler<SingleContextElement>? OnMouseExitedSingle;
        public event EventHandler<SingleContextElement>? OnMouseHoveringSingle;

        public event EventHandler<(GUIBoundKeyEventArgs, StackContextElement)>? OnKeyBindDownStack;
        public event EventHandler<StackContextElement>? OnMouseEnteredStack;

        public event EventHandler<ContextMenuElement>? OnExitedTree;

        public event EventHandler? OnCloseRootMenu;
        public event EventHandler<int>? OnCloseChildMenu;

        public ContextMenuView()
        {
            IoCManager.InjectDependencies(this);
            Menus = new Stack<ContextMenuPopup>();
            Elements = new Dictionary<IEntity, ContextMenuElement>();
        }

        public void AddRootMenu(List<IEntity> entities)
        {
            Elements = new Dictionary<IEntity, ContextMenuElement>(entities.Count);

            var rootContextMenu = new ContextMenuPopup();
            rootContextMenu.OnPopupHide += () => OnCloseRootMenu?.Invoke(this, EventArgs.Empty);
            Menus.Push(rootContextMenu);

            var entitySpriteStates = GroupEntities(entities);
            var orderedStates = entitySpriteStates.ToList();
            orderedStates.Sort((x, y) => string.CompareOrdinal(x.First().Prototype!.Name, y.First().Prototype!.Name));
            AddToUI(orderedStates);

            _userInterfaceManager.ModalRoot.AddChild(rootContextMenu);
            var size = rootContextMenu.List.CombinedMinimumSize;
            var box = UIBox2.FromDimensions(_userInterfaceManager.MousePositionScaled.Position, size);
            rootContextMenu.Open(box);
        }
        public void AddChildMenu(IEnumerable<IEntity> entities, Vector2 position, StackContextElement? stack)
        {
            if (stack == null) return;
            var newDepth = stack.ParentMenu?.Depth + 1 ?? 1;
            var childContextMenu = new ContextMenuPopup(newDepth);
            Menus.Push(childContextMenu);

            var orderedStates = GroupEntities(entities, newDepth);
            AddToUI(orderedStates, stack);

            _userInterfaceManager.ModalRoot.AddChild(childContextMenu);
            var size = childContextMenu.List.CombinedMinimumSize;
            childContextMenu.Open(UIBox2.FromDimensions(position + (stack.Width, 0), size));
        }

        private void AddToUI(List<List<IEntity>> entities, StackContextElement? stack = null)
        {
            if (entities.Count == 1)
            {
                foreach (var entity in entities[0])
                {
                    AddSingleContextElement(entity, stack);
                }
            }
            else
            {
                foreach (var entity in entities)
                {
                    if (entity.Count == 1)
                    {
                        AddSingleContextElement(entity[0], stack);
                    }
                    else
                    {
                        AddStackContextElement(entity, stack);
                    }
                }
            }
        }
        private void AddSingleContextElement(IEntity entity, StackContextElement? pre)
        {
            if (Menus.TryPeek(out var menu))
            {
                var single = new SingleContextElement(entity, pre, menu);

                single.OnKeyBindDown += args => OnKeyBindDownSingle?.Invoke(this, (args, single));
                single.OnMouseEntered += _ => OnMouseEnteredSingle?.Invoke(this, single);
                single.OnMouseExited += _ => OnMouseExitedSingle?.Invoke(this, single);
                single.OnMouseHovering += () => OnMouseHoveringSingle?.Invoke(this, single);
                single.OnExitedTree += () => OnExitedTree?.Invoke(this, single);

                UpdateElements(entity, single);
                menu.AddToMenu(single);
            }
        }
        private void AddStackContextElement(IEnumerable<IEntity> entities, StackContextElement? pre)
        {
            if (Menus.TryPeek(out var menu))
            {
                var stack = new StackContextElement(entities, pre, menu);

                stack.OnKeyBindDown += args => OnKeyBindDownStack?.Invoke(this, (args, stack));
                stack.OnMouseEntered += _ => OnMouseEnteredStack?.Invoke(this, stack);
                stack.OnExitedTree += () => OnExitedTree?.Invoke(this, stack);

                foreach (var entity in entities)
                {
                    UpdateElements(entity, stack);
                }
                menu.AddToMenu(stack);
            }
        }
        private void UpdateElements(IEntity entity, ContextMenuElement element)
        {
            if (Elements.ContainsKey(entity))
            {
                Elements[entity] = element;
            }
            else
            {
                Elements.Add(entity, element);
            }
        }

        private void RemoveFromUI(ContextMenuElement element)
        {
            var menu = element.ParentMenu;
            if (menu != null)
            {
                menu.RemoveFromMenu(element);
                if (menu.List.ChildCount == 0)
                {
                    OnCloseChildMenu?.Invoke(this, menu.Depth - 1);
                }
            }
        }
        public void RemoveEntity(IEntity entity)
        {
            var element = Elements[entity];
            switch (element)
            {
                case SingleContextElement singleContextElement:
                    RemoveFromUI(singleContextElement);
                    UpdateBranch(entity, singleContextElement.Pre);
                    break;
                case StackContextElement stackContextElement:
                    stackContextElement.RemoveEntity(entity);
                    if (stackContextElement.EntitiesCount == 0)
                    {
                        RemoveFromUI(stackContextElement);
                    }
                    UpdateBranch(entity, stackContextElement.Pre);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(element));
            }
            Elements.Remove(entity);
        }
        private void UpdateBranch(IEntity entity, StackContextElement? stack)
        {
            while (stack != null)
            {
                stack.RemoveEntity(entity);
                if (stack.EntitiesCount == 0)
                {
                    RemoveFromUI(stack);
                }

                stack = stack.Pre;
            }
        }

        public void UpdateParents(ContextMenuElement element)
        {
            switch (element)
            {
                case SingleContextElement singleContextElement:
                    if (singleContextElement.Pre != null)
                    {
                        Elements[singleContextElement.ContextEntity] = singleContextElement.Pre;
                    }

                    break;
                case StackContextElement stackContextElement:
                    if (stackContextElement.Pre != null)
                    {
                        foreach (var entity in stackContextElement.ContextEntities)
                        {
                            Elements[entity] = stackContextElement.Pre;
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(element));
            }
        }

        public void CloseContextPopups()
        {
            while (Menus.Count > 0)
            {
                Menus.Pop().Dispose();
            }

            Elements.Clear();
        }
        public void CloseContextPopups(int depth)
        {
            while (Menus.Count > 0 && Menus.Peek().Depth > depth)
            {
                Menus.Pop().Dispose();
            }
        }

        public void Dispose()
        {
            CloseContextPopups();
        }
    }
}

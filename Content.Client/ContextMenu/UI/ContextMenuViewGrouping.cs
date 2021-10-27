using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.ContextMenu.UI
{
    public partial class ContextMenuView
    {
        public const int GroupingTypesCount = 2;
        private int GroupingContextMenuType { get; set; }
        public void OnGroupingContextMenuChanged(int obj)
        {
            CloseContextPopups();
            GroupingContextMenuType = obj;
        }

        private List<List<IEntity>> GroupEntities(IEnumerable<IEntity> entities, int depth = 0)
        {
            if (GroupingContextMenuType == 0)
            {
                var newEntities = entities.GroupBy(e => e, new PrototypeContextMenuComparer()).ToList();
                return newEntities.Select(grp => grp.ToList()).ToList();
            }
            else
            {
                var newEntities = entities.GroupBy(e => e, new PrototypeAndStatesContextMenuComparer(depth)).ToList();
                return newEntities.Select(grp => grp.ToList()).ToList();
            }
        }

        private sealed class PrototypeAndStatesContextMenuComparer : IEqualityComparer<IEntity>
        {
            private static readonly List<Func<IEntity, IEntity, bool>> EqualsList = new()
            {
                (a, b) => a.Prototype!.ID == b.Prototype!.ID,
                (a, b) =>
                {
                    var xStates = a.GetComponent<ISpriteComponent>().AllLayers.Where(e => e.Visible).Select(s => s.RsiState.Name);
                    var yStates = b.GetComponent<ISpriteComponent>().AllLayers.Where(e => e.Visible).Select(s => s.RsiState.Name);

                    return xStates.OrderBy(t => t).SequenceEqual(yStates.OrderBy(t => t));
                },
            };
            private static readonly List<Func<IEntity, int>> GetHashCodeList = new()
            {
                e => EqualityComparer<string>.Default.GetHashCode(e.Prototype!.ID),
                e =>
                {
                    var hash = 0;
                    foreach (var element in e.GetComponent<ISpriteComponent>().AllLayers.Where(obj => obj.Visible).Select(s => s.RsiState.Name))
                    {
                        hash ^= EqualityComparer<string>.Default.GetHashCode(element!);
                    }
                    return hash;
                },
            };

            private static int Count => EqualsList.Count - 1;

            private readonly int _depth;
            public PrototypeAndStatesContextMenuComparer(int step = 0)
            {
                _depth = step > Count ? Count : step;
            }

            public bool Equals(IEntity? x, IEntity? y)
            {
                if (x == null)
                {
                    return y == null;
                }

                return y != null && EqualsList[_depth](x, y);
            }

            public int GetHashCode(IEntity e)
            {
                return GetHashCodeList[_depth](e);
            }
        }

        private sealed class PrototypeContextMenuComparer : IEqualityComparer<IEntity>
        {
            public bool Equals(IEntity? x, IEntity? y)
            {
                if (x == null)
                {
                    return y == null;
                }
                if (y != null)
                {
                    if (x.Prototype?.ID == y.Prototype?.ID)
                    {
                        var xStates = x.GetComponent<ISpriteComponent>().AllLayers.Where(e => e.Visible).Select(s => s.RsiState.Name);
                        var yStates = y.GetComponent<ISpriteComponent>().AllLayers.Where(e => e.Visible).Select(s => s.RsiState.Name);

                        return xStates.OrderBy(t => t).SequenceEqual(yStates.OrderBy(t => t));
                    }
                }
                return false;
            }

            public int GetHashCode(IEntity e)
            {
                var hash = EqualityComparer<string>.Default.GetHashCode(e.Prototype?.ID!);

                if (e.TryGetComponent<ISpriteComponent>(out var sprite))
                {
                    foreach (var element in sprite.AllLayers.Where(obj => obj.Visible).Select(s => s.RsiState.Name))
                    {
                        hash ^= EqualityComparer<string>.Default.GetHashCode(element!);
                    }
                }

                return hash;
            }
        }
    }
}

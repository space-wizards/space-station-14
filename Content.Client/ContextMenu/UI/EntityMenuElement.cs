using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.ContextMenu.UI
{
    public partial class EntityMenuElement : ContextMenuElement
    {
        public const string StyleClassEntityMenuCountText = "contextMenuCount";

        [Dependency] private IEntityManager _entityManager = default!;

        /// <summary>
        ///     The entity that can be accessed by interacting with this element.
        /// </summary>
        public EntityUid? Entity;

        /// <summary>
        ///     How many entities are accessible through this element's sub-menus.
        /// </summary>
        /// <remarks>
        ///     This is used for <see cref="CountLabel"/>
        /// </remarks>
        public int Count;

        public readonly Label CountLabel;
        public readonly SpriteView EntityIcon = new() { OverrideDirection = Direction.South};

        public EntityMenuElement(EntityUid? entity = null)
        {
            IoCManager.InjectDependencies(this);

            CountLabel = new Label { StyleClasses = { StyleClassEntityMenuCountText } };
            Icon.AddChild(new LayoutContainer() { Children = { EntityIcon, CountLabel } });

            LayoutContainer.SetAnchorPreset(CountLabel, LayoutContainer.LayoutPreset.BottomRight);
            LayoutContainer.SetGrowHorizontal(CountLabel, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetGrowVertical(CountLabel, LayoutContainer.GrowDirection.Begin);

            Entity = entity;
            if (Entity != default)
            {
                Count = 1;
                CountLabel.Visible = false;
                UpdateEntity();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Entity = default;
            Count = 0;
        }

        /// <summary>
        ///     Update the icon and text of this element based on the given entity or this element's own entity if none
        ///     is provided.
        /// </summary>
        public void UpdateEntity(EntityUid? entity = null)
        {
            // Deleted() automatically checks for null & existence.
            if (!_entityManager.Deleted(Entity))
                entity = Entity;

            if (_entityManager.Deleted(entity))
            {
                Text = string.Empty;
                return;
            }

            EntityIcon.Sprite = _entityManager.GetComponentOrNull<ISpriteComponent>(entity);

            if (UserInterfaceManager.DebugMonitors.Visible)
                Text = $"{_entityManager.GetComponent<MetaDataComponent>(entity.Value!).EntityName} ({entity})";
            else
                Text = _entityManager.GetComponent<MetaDataComponent>(entity.Value!).EntityName;
        }
    }
}

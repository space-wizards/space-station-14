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

        /// <summary>
        ///     The entity that can be accessed by interacting with this element.
        /// </summary>
        public EntityUid Entity;

        /// <summary>
        ///     How many entities are accessible through this element's sub-menus.
        /// </summary>
        /// <remarks>
        ///     This is used for <see cref="CountLabel"/>
        /// </remarks>
        public int Count;

        public readonly Label CountLabel;
        public readonly SpriteView EntityIcon = new() { OverrideDirection = Direction.South};

        public EntityMenuElement(EntityUid entity = default)
        {
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
        public void UpdateEntity(EntityUid entity = default)
        {
            if (Entity != default && IoCManager.Resolve<IEntityManager>().EntityExists(Entity) && !entity.Valid)
                entity = Entity;

            if (entity == default)
            {
                Text = string.Empty;
                return;
            }

            EntityIcon.Sprite = IoCManager.Resolve<IEntityManager>().GetComponentOrNull<ISpriteComponent>(entity);

            if (UserInterfaceManager.DebugMonitors.Visible)
                Text = $"{IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity!).EntityName} ({entity})";
            else
                Text = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity!).EntityName;
        }
    }
}

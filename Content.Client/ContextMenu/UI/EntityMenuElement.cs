using Content.Client.Stylesheets;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.ContextMenu.UI
{
    public partial class EntityMenuElement : ContextMenuElement
    {
        public const string StyleClassEntityMenuCountText = "contextMenuCount";

        /// <summary>
        ///     The entity that can be accessed by interacting with this element.
        /// </summary>
        public IEntity? Entity;

        /// <summary>
        ///     How many entities are accessible through this element's sub-menus.
        /// </summary>
        /// <remarks>
        ///     This is used for <see cref="CountLabel"/>
        /// </remarks>
        public int Count;

        public Label CountLabel;
        public SpriteView EntityIcon = new SpriteView { OverrideDirection = Direction.South};

        public EntityMenuElement(IEntity? entity = null) : base()
        {
            CountLabel = new Label { StyleClasses = { StyleClassEntityMenuCountText } };
            Icon.AddChild(new LayoutContainer() { Children = { EntityIcon, CountLabel } });

            LayoutContainer.SetAnchorPreset(CountLabel, LayoutContainer.LayoutPreset.BottomRight);
            LayoutContainer.SetGrowHorizontal(CountLabel, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetGrowVertical(CountLabel, LayoutContainer.GrowDirection.Begin);

            Entity = entity;
            if (Entity != null)
            {
                Count = 1;
                CountLabel.Visible = false;
                UpdateEntity();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Entity = null;
            Count = 0;
        }

        /// <summary>
        ///     Update the icon and text of this element based on the given entity or this element's own entity if none
        ///     is provided.
        /// </summary>
        public void UpdateEntity(IEntity? entity = null)
        {
            entity ??= Entity;

            EntityIcon.Sprite = entity?.GetComponentOrNull<ISpriteComponent>();

            if (UserInterfaceManager.DebugMonitors.Visible)
                Text = $"{entity?.Name} ({entity?.Uid})";
            else
                Text = entity?.Name ?? string.Empty;
        }
    }
}

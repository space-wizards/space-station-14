using System.Linq;
using Content.Client.Administration.Managers;
using Content.Client.Administration.Systems;
using Content.Shared.Administration;
using Content.Shared.IdentityManagement;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ContextMenu.UI
{
    public sealed partial class EntityMenuElement : ContextMenuElement
    {
        public const string StyleClassEntityMenuCountText = "contextMenuCount";

        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private AdminSystem _adminSystem;

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

            _adminSystem = _entityManager.System<AdminSystem>();

            CountLabel = new Label { StyleClasses = { StyleClassEntityMenuCountText } };
            Icon.AddChild(new LayoutContainer() { Children = { EntityIcon, CountLabel } });

            LayoutContainer.SetAnchorPreset(CountLabel, LayoutContainer.LayoutPreset.BottomRight);
            LayoutContainer.SetGrowHorizontal(CountLabel, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetGrowVertical(CountLabel, LayoutContainer.GrowDirection.Begin);

            Entity = entity;
            if (Entity == null)
                return;

            Count = 1;
            CountLabel.Visible = false;
            UpdateEntity();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Entity = null;
            Count = 0;
        }

        private string? SearchPlayerName(EntityUid entity)
        {
            return _adminSystem.PlayerList.FirstOrDefault(player => player.EntityUid == entity)?.Username;
        }

        private string GetEntityDescriptionAdmin(EntityUid entity)
        {
            var representation = _entityManager.ToPrettyString(entity);

            var name = representation.Name;
            var id = representation.Uid;
            var prototype = representation.Prototype;
            var playerName = representation.Session?.Name ?? SearchPlayerName(entity);
            var deleted = representation.Deleted;

            return $"{name} ({id}{(prototype != null ? $", {prototype}" : "")}{(playerName != null ? $", {playerName}" : "")}){(deleted ? "D" : "")}";
        }

        private string GetEntityDescription(EntityUid entity)
        {
            if (_adminManager.HasFlag(AdminFlags.Admin | AdminFlags.Debug))
            {
                return GetEntityDescriptionAdmin(entity);
            }

            return Identity.Name(entity, _entityManager, _playerManager.LocalPlayer!.ControlledEntity!);
        }

        /// <summary>
        ///     Update the icon and text of this element based on the given entity or this element's own entity if none
        ///     is provided.
        /// </summary>
        public void UpdateEntity(EntityUid? entity = null)
        {
            entity ??= Entity;

            // check whether entity is null, invalid, or has been deleted.
            // _entityManager.Deleted() implicitly checks all of these.
            if (_entityManager.Deleted(entity))
            {
                EntityIcon.Sprite = null;
                Text = string.Empty;
            }
            else
            {
                EntityIcon.Sprite = _entityManager.GetComponentOrNull<SpriteComponent>(entity);
                Text = GetEntityDescription(entity.Value);
            }
        }
    }
}

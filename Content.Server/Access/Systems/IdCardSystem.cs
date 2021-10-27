using Content.Server.Access.Components;
using Content.Shared.Access;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Access.Systems
{
    public class IdCardSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<IdCardComponent, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, IdCardComponent id, ComponentInit args)
        {
            id.OriginalOwnerName ??= id.Owner.Name;
            UpdateEntityName(uid, id);
        }

        public bool TryChangeJobTitle(EntityUid uid, string jobTitle, IdCardComponent? id = null)
        {
            if (!Resolve(uid, ref id))
                return false;

            // TODO: Whenever we get admin logging these should be logged
            if (jobTitle.Length > SharedIdCardConsoleComponent.MaxJobTitleLength)
                jobTitle = jobTitle[..SharedIdCardConsoleComponent.MaxJobTitleLength];

            id.JobTitle = jobTitle;
            UpdateEntityName(uid, id);
            return true;
        }

        public bool TryChangeFullName(EntityUid uid, string fullName, IdCardComponent? id = null)
        {
            if (!Resolve(uid, ref id))
                return false;

            if (fullName.Length > SharedIdCardConsoleComponent.MaxFullNameLength)
                fullName = fullName[..SharedIdCardConsoleComponent.MaxFullNameLength];

            id.FullName = fullName;
            UpdateEntityName(uid, id);
            return true;
        }

        /// <summary>
        /// Changes the <see cref="Entity.Name"/> of <see cref="Component.Owner"/>.
        /// </summary>
        /// <remarks>
        /// If either <see cref="FullName"/> or <see cref="JobTitle"/> is empty, it's replaced by placeholders.
        /// If both are empty, the original entity's name is restored.
        /// </remarks>
        private void UpdateEntityName(EntityUid uid, IdCardComponent? id = null)
        {
            if (!Resolve(uid, ref id))
                return;

            if (string.IsNullOrWhiteSpace(id.FullName) && string.IsNullOrWhiteSpace(id.JobTitle))
            {
                id.Owner.Name = id.OriginalOwnerName;
                return;
            }

            var jobSuffix = string.IsNullOrWhiteSpace(id.JobTitle) ? string.Empty : $" ({id.JobTitle})";

            id.Owner.Name = string.IsNullOrWhiteSpace(id.FullName)
                ? Loc.GetString("access-id-card-component-owner-name-job-title-text",
                                ("originalOwnerName", id.OriginalOwnerName),
                                ("jobSuffix", jobSuffix))
                : Loc.GetString("access-id-card-component-owner-full-name-job-title-text",
                                ("fullName", id.FullName),
                                ("jobSuffix", jobSuffix));
        }
    }
}

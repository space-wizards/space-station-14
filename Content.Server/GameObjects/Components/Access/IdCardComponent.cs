using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Access
{
    [RegisterComponent]
    public class IdCardComponent : Component
    {
        public override string Name => "IdCard";

        /// See <see cref="UpdateEntityName"/>.
        private string _ownerOriginalName;

        private string _fullName;
        [ViewVariables(VVAccess.ReadWrite)]
        public string FullName
        {
            get => _fullName;
            set
            {
                _fullName = value;
                UpdateEntityName();
            }
        }

        private string _jobTitle;
        [ViewVariables(VVAccess.ReadWrite)]
        public string JobTitle
        {
            get => _jobTitle;
            set
            {
                _jobTitle = value;
                UpdateEntityName();
            }
        }

        /// <summary>
        /// Changes the <see cref="Entity.Name"/> of <see cref="Component.Owner"/>.
        /// </summary>
        /// <remarks>
        /// If either <see cref="FullName"/> or <see cref="JobTitle"/> is empty, it's replaced by placeholders.
        /// If both are empty, the original entity's name is restored.
        /// </remarks>
        private void UpdateEntityName()
        {
            if (string.IsNullOrWhiteSpace(FullName) && string.IsNullOrWhiteSpace(JobTitle))
            {
                Owner.Name = _ownerOriginalName;
                return;
            }

            var jobSuffix = string.IsNullOrWhiteSpace(JobTitle) ? "" : $" ({JobTitle})";

            if (string.IsNullOrWhiteSpace(FullName))
            {
                Owner.Name = Loc.GetString("{0}{1}", _ownerOriginalName, jobSuffix);
            }
            else
            {
                Owner.Name = Loc.GetString("{0}'s ID card{1}", FullName, jobSuffix);
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            _ownerOriginalName ??= Owner.Name;
            UpdateEntityName();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _fullName, "fullName", string.Empty);
            serializer.DataField(ref _jobTitle, "jobTitle", string.Empty);
            serializer.DataField(ref _ownerOriginalName, "originalOwnerName", null);
        }
    }
}

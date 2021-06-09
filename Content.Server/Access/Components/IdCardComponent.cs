using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Access.Components
{
    [RegisterComponent]
    public class IdCardComponent : Component
    {
        public override string Name => "IdCard";

        /// See <see cref="UpdateEntityName"/>.
        [DataField("originalOwnerName")]
        private string _originalOwnerName = default!;

        [DataField("fullName")]
        private string? _fullName;
        [ViewVariables(VVAccess.ReadWrite)]
        public string? FullName
        {
            get => _fullName;
            set
            {
                _fullName = value;
                UpdateEntityName();
            }
        }

        [DataField("jobTitle")]
        private string? _jobTitle;
        [ViewVariables(VVAccess.ReadWrite)]
        public string? JobTitle
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
                Owner.Name = _originalOwnerName;
                return;
            }

            var jobSuffix = string.IsNullOrWhiteSpace(JobTitle) ? "" : $" ({JobTitle})";

            Owner.Name = string.IsNullOrWhiteSpace(FullName)
                ? Loc.GetString("{0}{1}", _originalOwnerName, jobSuffix)
                : Loc.GetString("{0}'s ID card{1}", FullName, jobSuffix);
        }

        public override void Initialize()
        {
            base.Initialize();
            // ReSharper disable once ConstantNullCoalescingCondition
            _originalOwnerName ??= Owner.Name;
            UpdateEntityName();
        }
    }
}

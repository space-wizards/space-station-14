#nullable enable
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Access
{
    [RegisterComponent]
    public class PresetIdCardComponent : Component, IMapInit
    {
        public override string Name => "PresetIdCard";

        [DataField("job")]
        private string? _jobName;

        void IMapInit.MapInit()
        {
            if (_jobName == null)
            {
                return;
            }

            var prototypes = IoCManager.Resolve<IPrototypeManager>();
            var job = prototypes.Index<JobPrototype>(_jobName);
            var access = Owner.GetComponent<AccessComponent>();
            var idCard = Owner.GetComponent<IdCardComponent>();

            access.SetTags(job.Access);
            idCard.JobTitle = job.Name;
        }
    }
}

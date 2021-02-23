#nullable enable
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Access
{
    [RegisterComponent]
    public class PresetIdCardComponent : Component, IMapInit
    {
        public override string Name => "PresetIdCard";

        private string? _jobName;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _jobName, "job", null);
        }

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

using Content.Server.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using System;

namespace Content.Server.Access.Systems
{
    public class PresetIdCardSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IdCardSystem _cardSystem = default!;
        [Dependency] private readonly AccessSystem _accessSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PresetIdCardComponent, MapInitEvent>(OnMapInit);
        }

        private void OnMapInit(EntityUid uid, PresetIdCardComponent id, MapInitEvent args)
        {
            if (id.JobName == null) return;

            if (!_prototypeManager.TryIndex(id.JobName, out JobPrototype? job))
            {
                Logger.ErrorS("access", $"Invalid job id ({id.JobName}) for preset card");
                return;
            }

            // set access for access component
            _accessSystem.TrySetTags(uid, job.Access);

            // and also change job title on a card id
            _cardSystem.TryChangeJobTitle(uid, job.Name);
        }
    }
}

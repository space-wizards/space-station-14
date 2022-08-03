using Content.Shared.Body.Components;
using Content.Shared.Body.Systems.Part;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems.Body
{
    public abstract partial class SharedBodySystem : EntitySystem
    {
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
        [Dependency] protected readonly SharedBodyPartSystem BodyPartSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedBodyComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<SharedBodyComponent, ComponentGetState>(OnComponentGetState);
        }

        protected virtual void OnComponentInit(EntityUid uid, SharedBodyComponent component, ComponentInit args)
        {
            UpdateFromTemplate(uid, component.TemplateId, component);
        }

        public void OnComponentGetState(EntityUid uid, SharedBodyComponent body, ref ComponentGetState args)
        {
            args.State = new BodyComponentState(body.Slots);
        }
    }
}

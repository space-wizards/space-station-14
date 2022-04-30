using Content.Server.Body.Components;
using Content.Server.Mind.Components;

namespace Content.Server.Body.Systems
{
    public sealed class TransferMindOnGibSystem : EntitySystem
    {
        [Dependency] private readonly SkeletonBodyManagerSystem _skeletonBodyManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TransferMindOnGibComponent, BeforeGibbedEvent>(OnBeforeGibbed);
            SubscribeLocalEvent<TransferMindOnGibComponent, BeingGibbedEvent>(OnBeingGibbed);
        }

        private void OnBeforeGibbed(EntityUid uid, TransferMindOnGibComponent component, BeforeGibbedEvent args)
        {
            if (TryComp<MindComponent>(uid, out var mindComp) && mindComp.Mind != null)
            {
                if (TryComp<BodyComponent>(uid, out var bodyComp))
                {
                    foreach (var part in bodyComp.Parts)
                    {
                        var entity = part.Key.Owner;
                        if (HasComp<SkeletonBodyManagerComponent>(entity))
                        {
                            component.TransferTarget = entity;
                            _skeletonBodyManager.UpdateDNAEntry(entity, uid);
                            mindComp.Mind.TransferTo(entity);
                        }
                    }
                }
            }
        }

        /// <remarks>
        /// This information has to be collected in this system because the BeingGibbedEvent
        /// is raised locally on the entity
        /// </remarks>
        private void OnBeingGibbed(EntityUid uid, TransferMindOnGibComponent component, BeingGibbedEvent args)
        {
            if (TryComp<SkeletonBodyManagerComponent>(component.TransferTarget, out var comp))
            {
                comp.BodyParts = args.GibbedParts;
            }
        }
    }
}

using Content.Server.DoAfter;
using Content.Server.Engineering.Components;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Engineering.EntitySystems
{
    [UsedImplicitly]
    public class DisassembleOnActivateSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisassembleOnActivateComponent, ActivateInWorldEvent>(HandleActivateInWorld);
        }

        private async void HandleActivateInWorld(EntityUid uid, DisassembleOnActivateComponent component, ActivateInWorldEvent args)
        {
            if (string.IsNullOrEmpty(component.Prototype))
                return;
            if (!args.User.InRangeUnobstructed(args.Target))
                return;

            if (component.DoAfterTime > 0 && TryGet<DoAfterSystem>(out var doAfterSystem))
            {
                var doAfterArgs = new DoAfterEventArgs(args.User, component.DoAfterTime, component.TokenSource.Token)
                {
                    BreakOnUserMove = true,
                    BreakOnStun = true,
                };
                var result = await doAfterSystem.WaitDoAfter(doAfterArgs);

                if (result != DoAfterStatus.Finished)
                    return;
                component.TokenSource.Cancel();
            }

            if (component.Deleted || (!IoCManager.Resolve<IEntityManager>().EntityExists(component.Owner) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(component.Owner).EntityLifeStage) >= EntityLifeStage.Deleted)
                return;

            var entity = EntityManager.SpawnEntity(component.Prototype, IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(component.Owner).Coordinates);

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent<HandsComponent?>(args.User, out var hands)
                && IoCManager.Resolve<IEntityManager>().TryGetComponent<ItemComponent?>(entity, out var item))
            {
                hands.PutInHandOrDrop(item);
            }

            IoCManager.Resolve<IEntityManager>().DeleteEntity((EntityUid) component.Owner);

            return;
        }
    }
}

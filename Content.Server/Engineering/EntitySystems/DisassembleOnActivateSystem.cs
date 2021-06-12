using Content.Server.DoAfter;
using Content.Server.Engineering.Components;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

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

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<DisassembleOnActivateComponent, ActivateInWorldEvent>(HandleActivateInWorld);
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
                var result = await doAfterSystem.DoAfter(doAfterArgs);

                if (result != DoAfterStatus.Finished)
                    return;
                component.TokenSource.Cancel();
            }

            if (component.Deleted || component.Owner.Deleted)
                return;

            var entity = EntityManager.SpawnEntity(component.Prototype, component.Owner.Transform.Coordinates);

            if (args.User.TryGetComponent<HandsComponent>(out var hands)
                && entity.TryGetComponent<ItemComponent>(out var item))
            {
                hands.PutInHandOrDrop(item);
            }

            component.Owner.Delete();

            return;
        }
    }
}

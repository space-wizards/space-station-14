using Content.Server.GameObjects.Components.Engineering;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using System.Threading;

namespace Content.Server.GameObjects.EntitySystems
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

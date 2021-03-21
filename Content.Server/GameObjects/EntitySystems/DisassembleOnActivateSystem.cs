using Content.Server.GameObjects.Components.Engineering;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class DisassembleOnActivateSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisassembleOnActivateComponent, ActivateInWorldMessage>(HandleActivateInWorld);
        }

        private async void HandleActivateInWorld(EntityUid uid, DisassembleOnActivateComponent component, ActivateInWorldMessage args)
        {
            if (component.Prototype == null)
                return;
            if (!args.User.InRangeUnobstructed(args.Activated))
                return;

            if (component.DoAfterTime > 0 && TryGet<DoAfterSystem>(out var doAfterSystem))
            {
                var doAfterArgs = new DoAfterEventArgs(args.User, component.DoAfterTime)
                {
                    BreakOnUserMove = true,
                    BreakOnStun = true,
                };
                var result = await doAfterSystem.DoAfter(doAfterArgs);

                if (result != DoAfterStatus.Finished)
                    return;
            }

            EntityManager.SpawnEntity(component.Prototype, component.Owner.Transform.Coordinates);

            component.Owner.Delete();

            return;
        }
    }
}

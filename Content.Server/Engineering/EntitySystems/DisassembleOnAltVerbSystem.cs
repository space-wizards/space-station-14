using Content.Server.DoAfter;
using Content.Server.Engineering.Components;
using Content.Server.Hands.Components;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Item;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using JetBrains.Annotations;
namespace Content.Server.Engineering.EntitySystems
{
    [UsedImplicitly]
    public class DisassembleOnAltVerbSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisassembleOnAltVerbComponent, GetAlternativeVerbsEvent>(AddDisassembleVerb);
        }
        private void AddDisassembleVerb(EntityUid uid, DisassembleOnAltVerbComponent component, GetAlternativeVerbsEvent args)
        {
         if (!args.CanInteract)
                return;

            Verb verb = new()
            {
                Act = () =>
                {
                    AttemptDisassemble(uid, args.User, args.Target, component);
                },
                Text = Loc.GetString("disassemble-system-verb-disassemble"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        public async void AttemptDisassemble(EntityUid uid, EntityUid user, EntityUid target, DisassembleOnAltVerbComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;
            if (string.IsNullOrEmpty(component.Prototype))
                return;
            if (!user.InRangeUnobstructed(target))
                return;

            if (component.DoAfterTime > 0 && TryGet<DoAfterSystem>(out var doAfterSystem))
            {
                var doAfterArgs = new DoAfterEventArgs(user, component.DoAfterTime, component.TokenSource.Token)
                {
                    BreakOnUserMove = true,
                    BreakOnStun = true,
                };
                var result = await doAfterSystem.WaitDoAfter(doAfterArgs);

                if (result != DoAfterStatus.Finished)
                    return;
                component.TokenSource.Cancel();
            }

            if (component.Deleted || Deleted(component.Owner))
                return;

            if (!TryComp<TransformComponent>(component.Owner, out var transformComp))
                return;

            var entity = EntityManager.SpawnEntity(component.Prototype, transformComp.Coordinates);

            if (TryComp<HandsComponent?>(user, out var hands)
                && TryComp<SharedItemComponent?>(entity, out var item))
            {
                hands.PutInHandOrDrop(item);
            }

            EntityManager.DeleteEntity(component.Owner);

            return;
        }
    }
}

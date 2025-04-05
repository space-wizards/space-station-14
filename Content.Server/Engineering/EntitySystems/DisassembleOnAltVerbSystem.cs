using Content.Server.Engineering.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Content.Shared.Engineering.EntitySystems;

namespace Content.Server.Engineering.EntitySystems
{
    [UsedImplicitly]
    public sealed class DisassembleOnAltVerbSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisassembleOnAltVerbComponent, GetVerbsEvent<AlternativeVerb>>(AddDisassembleVerb);
            SubscribeLocalEvent<DisassembleOnAltVerbComponent, DisassembleOnAltVerbDoAfterEvent>(OnDisassembleOnAltVerb);
        }
        private void AddDisassembleVerb(EntityUid uid, DisassembleOnAltVerbComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || args.Hands == null)
                return;

            AlternativeVerb verb = new()
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

            if (component.DoAfterTime > 0)
            {
                var doAfterArgs = new DoAfterArgs(EntityManager, user, component.DoAfterTime, new DisassembleOnAltVerbDoAfterEvent(), target)
                {
                    BreakOnMove = true,
                };

                if (!EntityManager.System<SharedDoAfterSystem>().TryStartDoAfter(doAfterArgs))
                {
                    return;
                }

            }

        }

        private void OnDisassembleOnAltVerb(EntityUid uid, DisassembleOnAltVerbComponent component, DisassembleOnAltVerbDoAfterEvent args)
        {
            if (component.Deleted || Deleted(uid))
                return;

            if (!TryComp(uid, out TransformComponent? transformComp))
                return;

            var entity = EntityManager.SpawnEntity(component.Prototype, transformComp.Coordinates);

            _handsSystem.TryPickup(args.User, entity);

            EntityManager.DeleteEntity(uid);
        }
    }
}

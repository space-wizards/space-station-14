using Content.Shared.Engineering.Components;
using Content.Shared.DoAfter;
using Content.Shared.Verbs;

namespace Content.Shared.Engineering.EntitySystems;

public sealed class SharedDisassembleOnAltVerbSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DisassembleOnAltVerbComponent, GetVerbsEvent<AlternativeVerb>>(AddDisassembleVerb);
    }
    private void AddDisassembleVerb(Entity<DisassembleOnAltVerbComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        // Doafter setup
        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            entity.Comp.DisassembleTime,
            new DissembleDoAfterEvent(),
            entity,
            entity)
        {
            BreakOnMove = true,
        };

        // Actual verb stuff
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                _doAfter.TryStartDoAfter(doAfterArgs);
            },
            Text = Loc.GetString("disassemble-system-verb-disassemble"),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }
}

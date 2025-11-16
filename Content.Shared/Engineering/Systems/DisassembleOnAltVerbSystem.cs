using Content.Shared.DoAfter;
using Content.Shared.Engineering.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Network;

namespace Content.Shared.Engineering.Systems;

public sealed partial class DisassembleOnAltVerbSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisassembleOnAltVerbComponent, GetVerbsEvent<AlternativeVerb>>(AddDisassembleVerb);
        SubscribeLocalEvent<DisassembleOnAltVerbComponent, DisassembleDoAfterEvent>(OnDisassembleDoAfter);
    }

    public void StartDisassembly(Entity<DisassembleOnAltVerbComponent> entity, EntityUid user)
    {
        // Doafter setup
        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            entity.Comp.DisassembleTime,
            new DisassembleDoAfterEvent(),
            entity,
            entity)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void AddDisassembleVerb(Entity<DisassembleOnAltVerbComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        var user = args.User;

        // Actual verb stuff
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                StartDisassembly(entity, user);
            },
            Text = Loc.GetString("disassemble-system-verb-disassemble"),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }

    private void OnDisassembleDoAfter(Entity<DisassembleOnAltVerbComponent> entity, ref DisassembleDoAfterEvent args)
    {
        if (!_net.IsServer || args.Cancelled) // This is odd but it works :)
            return;

        if (TrySpawnNextTo(entity.Comp.PrototypeToSpawn, entity.Owner, out var spawnedEnt))
            _handsSystem.TryPickup(args.User, spawnedEnt.Value);

        QueueDel(entity.Owner);
    }
}

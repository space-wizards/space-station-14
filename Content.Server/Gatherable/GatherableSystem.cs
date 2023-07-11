using Content.Server.Destructible;
using Content.Server.Gatherable.Components;
using Content.Shared.DoAfter;
using Content.Shared.Gatherable;
using Content.Shared.Interaction;
using Robust.Shared.Audio;

namespace Content.Server.Gatherable;

public sealed partial class GatherableSystem : EntitySystem
{
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GatherableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<GatherableComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<GatherableComponent, GatherableDoAfterEvent>(OnDoAfter);
        InitializeProjectile();
    }

    private void OnInteractUsing(EntityUid uid, GatherableComponent component, InteractUsingEvent args)
    {
        if (!TryComp<GatheringToolComponent>(args.Used, out var tool) || component.ToolWhitelist?.IsValid(args.Used) == false)
            return;

        // Can't gather too many entities at once.
        if (tool.MaxGatheringEntities < tool.GatheringEntities.Count + 1)
            return;

        var damageRequired = _destructible.DestroyedAt(uid);
        var damageTime = (damageRequired / tool.Damage.Total).Float();
        damageTime = Math.Max(1f, damageTime);

        var doAfter = new DoAfterArgs(args.User, damageTime, new GatherableDoAfterEvent(), uid, target: uid, used: args.Used)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.25f,
            DuplicateCondition = DuplicateConditions.SameTarget,
        };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private void OnInteractHand(EntityUid uid, GatherableComponent component, InteractHandEvent args)
    {
        if (component.ToolWhitelist?.Tags?.Contains("Hand") == false)
            return;

        var doAfter = new DoAfterArgs(args.User, TimeSpan.FromSeconds(component.HarvestTime), new GatherableDoAfterEvent(), uid, target: uid)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.25f,
            DuplicateCondition = DuplicateConditions.SameTarget,
        };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private void OnDoAfter(EntityUid uid, GatherableComponent component, GatherableDoAfterEvent args)
    {
        if (TryComp<GatheringToolComponent>(args.Args.Used, out var tool))
            tool.GatheringEntities.Remove(uid);

        if (args.Handled || args.Cancelled)
            return;

        Gather(uid, args.Args.Used, component, tool?.GatheringSound, args.Args.User);
        args.Handled = true;
    }

    public void Gather(EntityUid uid, EntityUid? gatherTool, GatherableComponent? component = null, SoundSpecifier? sound = null, EntityUid? gatherUser = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // Complete the gathering process
        _destructible.DestroyEntity(uid, gatherTool, gatherUser);
        _audio?.PlayPvs(sound, uid);
    }
}

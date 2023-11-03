

using Content.Shared.InfectorDead;
using Content.Shared.Interaction;
using Content.Shared.Bed.Sleep;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.InfectionDead.Components;
using Robust.Shared.Utility;
using Content.Shared.InfectorDead.Components;
using Content.Shared.InfectorDead;
using Robust.Shared.Audio;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;

namespace Content.Server.InfectorDead.EntitySystems
{
public sealed partial class InfectorDeadSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;

    private HashSet<EntityUid> _toUpdate = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InfectorDeadComponent, InteractNoHandEvent>(OnInteract);
        SubscribeLocalEvent<InfectorDeadComponent, InfectorDeadDoAfterEvent>(OnDoAfter);
    }

    private void OnInteract(EntityUid uid, InfectorDeadComponent component, InteractNoHandEvent args)
    {
        if (args.Target == args.User || args.Target == null)
            return;

        var target = args.Target.Value;

        if (!HasComp<MobStateComponent>(target) || !HasComp<HumanoidAppearanceComponent>(target) || HasComp<InfectorDeadComponent>(target) || HasComp<ImmunitetInfectionDeadComponent>(target))
            return;

        if (HasComp<InfectionDeadComponent>(target))
            return;

        BeginInfected(uid, target, component);

        args.Handled = true;

    }


    private void BeginInfected(EntityUid uid, EntityUid target, InfectorDeadComponent component)
    {
        var searchDoAfter = new DoAfterArgs(EntityManager, uid, component.InfectedDuration, new InfectorDeadDoAfterEvent(), uid, target: target)
        {
            DistanceThreshold = 2
        };



        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;


    }


    private void OnDoAfter(EntityUid uid, InfectorDeadComponent component, InfectorDeadDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (_mobState.IsDead(args.Args.Target.Value))
                {
                    _audio.PlayPvs("/Audio/Effects/Fluids/splat.ogg", args.Args.Target.Value, AudioParams.Default.WithVariation(0.2f).WithVolume(-4f));
                    _bodySystem.GibBody(args.Args.Target.Value);
                    Spawn(component.ArmyMobSpawnId, Transform(args.Args.Target.Value).Coordinates);
                    args.Handled = true;
                    return;
                }
        EnsureComp<InfectionDeadComponent>(args.Args.Target.Value);
        _toUpdate.Add(args.Args.Target.Value);

        args.Handled = true;
    }

}
}

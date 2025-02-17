// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.ReviveImplant;
using Content.Shared.Interaction.Events;
using Robust.Server.GameObjects;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.EntitySystems;
using System.Threading.Tasks;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Server.DeadSpace.Implants.Revive.Components;

namespace Content.Server.DeadSpace.Implants.Revive;

public sealed partial class ReviveImplantSystem : EntitySystem
{
    private Dictionary<EntityUid, bool> _isReviving = new();

    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReviveImplantComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ReviveImplantComponent, ReviveImplantActivateEvent>(OnDoAfter);
        SubscribeLocalEvent<ReviveImplantComponent, MobStateChangedEvent>(OnMobStateCritical);
    }

    private void OnUseInHand(EntityUid uid, ReviveImplantComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<ReviveImplantComponent>(args.User))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.InjectTime, new ReviveImplantActivateEvent(), uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);

        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, ReviveImplantComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        EnsureComp<ReviveImplantComponent>(args.Args.User);
        TransformToItem(uid, component);
        _audio.PlayPvs(component.ImplantedSound, args.User, AudioParams.Default.WithVolume(0.5f));
    }

    private void TransformToItem(EntityUid item, ReviveImplantComponent component)
    {
        var position = _transform.GetMapCoordinates(item);
        Del(item);
        Spawn("AutosurgeonAfter", position);
    }

    private async void OnMobStateCritical(EntityUid uid, ReviveImplantComponent component, MobStateChangedEvent args)
    {
        if (!TryComp<MobStateComponent>(uid, out var mobState) || mobState.CurrentState != MobState.Critical)
            return;

        if (_isReviving.TryGetValue(uid, out var reviving) && reviving)
            return;

        _isReviving[uid] = true;

        try
        {
            while (args.NewMobState == MobState.Critical)
            {
                if (mobState.CurrentState != MobState.Critical)
                    break;

                var reagents = new List<(string, FixedPoint2)>()
            {
                ("Epinephrine", 2.5f),
                ("Saline", 2.5f),
                ("Omnizine", 1f)
            };
                TryInjectReagents(uid, reagents);
                await Task.Delay(5000);
            }
        }
        finally
        {
            _isReviving[uid] = false;
        }
    }

    public bool TryInjectReagents(EntityUid uid, List<(string, FixedPoint2)> reagents)
    {
        var solution = new Shared.Chemistry.Components.Solution();
        foreach (var reagent in reagents)
        {
            solution.AddReagent(reagent.Item1, reagent.Item2);
        }

        if (!_solution.TryGetInjectableSolution(uid, out var targetSolution, out var _))
            return false;

        if (!_solution.TryAddSolution(targetSolution.Value, solution))
            return false;

        return true;
    }
}

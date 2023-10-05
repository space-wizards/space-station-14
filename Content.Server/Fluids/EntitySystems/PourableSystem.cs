using Content.Server.Fluids.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Shared.FixedPoint;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Content.Shared.Fluids.Components;
using Robust.Shared.Utility;

namespace Content.Server.Fluids.EntitySystems;

public sealed class PourableSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PourableComponent, GetVerbsEvent<Verb>>(AddPouringVerb);
    }

    private void AddPouringVerb(EntityUid uid, PourableComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using == null)
            return;

        if (!TryComp(args.Using, out SpillableComponent? spillable) ||
            !TryComp(args.Target, out PourableComponent? tank))
            return;

        Verb verb = new()
        {
            Text = Loc.GetString("drain-component-empty-verb-inhand", ("object", Name(args.Using.Value))),
            Act = () =>
            {
                Pouring(args.Using.Value, args.Target, tank);
            },
            Impact = LogImpact.Low,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png"))

        };
        args.Verbs.Add(verb);
    }

    private void Pouring(EntityUid container, EntityUid target, PourableComponent tank)
    {
        // Find the solution in the container that is emptied
        if (!_solutionSystem.TryGetDrainableSolution(container, out var containerSolution) ||
            containerSolution.Volume == FixedPoint2.Zero)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("drain-component-empty-verb-using-is-empty-message", ("object", container)),
                container);
            return;
        }

        // try to find the drain's solution
        if (!_solutionSystem.TryGetSolution(target, tank.SolutionName, out var tankSolution))
        {
            return;
        }

        // Try to transfer as much solution as possible to the drain

        var transferSolution = _solutionSystem.SplitSolution(container, containerSolution,
            FixedPoint2.Min(containerSolution.Volume, tankSolution.AvailableVolume));

        _solutionSystem.TryAddSolution(target, tankSolution, transferSolution);

        _audioSystem.PlayPvs(tank.ManualPouringSound, target);
        _ambientSoundSystem.SetAmbience(target, true);

        // If drain is full, spill
        if (tankSolution.MaxVolume == tankSolution.Volume)
        {
            _puddleSystem.TrySpillAt(Transform(target).Coordinates, containerSolution, out var puddle);
            _popupSystem.PopupEntity(
                Loc.GetString("drain-component-empty-verb-target-is-full-message", ("object", target)),
                container);
        }
    }
}

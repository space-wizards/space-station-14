using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Shared.FixedPoint;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Content.Shared.Fluids.Components;
using Robust.Shared.Utility;
using Content.Shared.Chemistry.Components;

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

        if (!TryComp(args.Using, out SolutionTransferComponent? transferAmountForUsing) ||
            !TryComp(args.Target, out PourableComponent? tank))
        {
            return;
        }

        Verb verb = new()
        {
            Text = Loc.GetString("pourable-component-transfer-menu-verb", ("target", args.Target)),
            Act = () =>
            {
                Pouring(args.Using.Value, transferAmountForUsing, args.Target, tank);
            },
            Impact = LogImpact.Low,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png"))
        };
        args.Verbs.Add(verb);
    }

    private void Pouring(EntityUid container, SolutionTransferComponent containerTransferAmount,
        EntityUid target, PourableComponent tank)
    {
        // Find the solution in the container that is emptied
        if (!_solutionSystem.TryGetDrainableSolution(container, out var containerSolution) ||
            containerSolution.Volume == FixedPoint2.Zero)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("pourable-component-empty-verb-using-is-empty-message", ("object", container)),
                container);
            return;
        }

        if (!_solutionSystem.TryGetSolution(target, tank.SolutionName, out var tankSolution))
        {
            return;
        }

        // Try to transfer solutions for transferAmount container value
        var transferAmount = containerTransferAmount.TransferAmount;

        var transferSolution = _solutionSystem.SplitSolution(container, containerSolution,
            FixedPoint2.Min(containerSolution.Volume, tankSolution.AvailableVolume, transferAmount));

        if (!_solutionSystem.TryAddSolution(target, tankSolution, transferSolution))
        {
            return;
        }

        _popupSystem.PopupEntity(
            Loc.GetString("pourable-component-transfer-solution-in-target-message",
            ("target", target),
            ("amount", transferSolution.Volume)),
            container);

        _audioSystem.PlayPvs(tank.ManualPouringSound, target);

        // If drain is full, spill
        if (tankSolution.MaxVolume == tankSolution.Volume)
            _puddleSystem.TrySpillAt(Transform(target).Coordinates, containerSolution, out var puddle);
    }
}

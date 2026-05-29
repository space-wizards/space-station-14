using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;
using Content.Shared.Temperature;

namespace Content.Server.Nutrition.EntitySystems
{
    public sealed partial class SmokingSystem
    {
        private void InitializeCigars()
        {
            SubscribeLocalEvent<CigarComponent, ActivateInWorldEvent>(OnCigarActivatedEvent);
            SubscribeLocalEvent<CigarComponent, InteractUsingEvent>(OnCigarInteractUsingEvent);
            SubscribeLocalEvent<CigarComponent, SmokableSolutionEmptyEvent>(OnCigarSolutionEmptyEvent);
            SubscribeLocalEvent<CigarComponent, AfterInteractEvent>(OnCigarAfterInteract);
        }

        private void OnCigarActivatedEvent(Entity<CigarComponent> entity, ref ActivateInWorldEvent args)
        {
            if (args.Handled || !args.Complex)
                return;

            if (!TryComp(entity, out SmokableComponent? smokable))
                return;

            if (smokable.State != SmokableState.Lit)
                return;

            SetSmokableState(entity, SmokableState.Burnt, smokable);
            args.Handled = true;
        }

        private void OnCigarInteractUsingEvent(Entity<CigarComponent> entity, ref InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp(entity, out SmokableComponent? smokable))
                return;

            if (smokable.State != SmokableState.Unlit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used, isHotEvent, false);

            if (!isHotEvent.IsHot)
                return;

            SetSmokableState(entity, SmokableState.Lit, smokable);
            args.Handled = true;
        }

        public void OnCigarAfterInteract(Entity<CigarComponent> entity, ref AfterInteractEvent args)
        {
            var targetEntity = args.Target;
            if (targetEntity == null) return;

            if (targetEntity == null ||
                !args.CanReach ||
                !TryComp(entity, out SmokableComponent? smokable) ||
                smokable.State == SmokableState.Lit)
                return;

            //Dippable
            if (
                args.CanReach &&
                TryComp(targetEntity, out DrainableSolutionComponent? drainable))
            {
                if (!_solutionContainerSystem.TryGetSolution(targetEntity.Value, drainable.Solution, out var containerSoln, out var containerSolution))
                    return;


                if (containerSolution.Volume <= 0)
                {
                    _popupSystem.PopupEntity(Loc.GetString("cigar-component-dip-empty", ("cigar", entity.Owner)), targetEntity.Value, args.User);
                    args.Handled = true;
                    return;
                }

                if (_solutionContainerSystem.TryGetSolution(entity.Owner, smokable.Solution, out var cigSoln, out var cigSolution))
                {

                    if (cigSolution.Volume >= cigSolution.MaxVolume)
                    {
                        _popupSystem.PopupEntity(Loc.GetString("cigar-component-dip-full", ("cigar", entity.Owner)), targetEntity.Value, args.User);
                        args.Handled = true;
                        return;
                    }

                    var maxToDrain = 10;
                    var amountToDrain = FixedPoint2.Min(cigSolution.AvailableVolume, maxToDrain);

                    var drawn = _solutionContainerSystem.SplitSolution(containerSoln.Value, amountToDrain);

                    //fill the cigarette
                    _solutionContainerSystem.TryAddSolution(cigSoln.Value, drawn);

                    _popupSystem.PopupEntity(Loc.GetString("cigar-component-dip-success", ("cigar", entity.Owner), ("target", targetEntity.Value)), targetEntity.Value, args.User);
                }

                args.Handled = true;
                return;
            }

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(targetEntity.Value, isHotEvent, true);

            if (!isHotEvent.IsHot)
                return;

            SetSmokableState(entity, SmokableState.Lit, smokable);
            args.Handled = true;
        }

        private void OnCigarSolutionEmptyEvent(Entity<CigarComponent> entity, ref SmokableSolutionEmptyEvent args)
        {
            SetSmokableState(entity, SmokableState.Burnt);
        }
    }
}

using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Placeable;
using Content.Shared.Hands; // This should be at the top of the file, not inside the method.
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server.Chemistry.EntitySystems
{
    public sealed class SolutionHeaterSystem : EntitySystem
    {
        [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SolutionHeaterComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<SolutionHeaterComponent, ItemPlacedEvent>(OnItemPlaced);
            SubscribeLocalEvent<SolutionHeaterComponent, ItemRemovedEvent>(OnItemRemoved);
            SubscribeLocalEvent<SolutionHeaterComponent, GotEquippedHandEvent>(OnHandPickUp);
        }

        private void TurnOn(EntityUid uid)
        {
            _appearance.SetData(uid, SolutionHeaterVisuals.IsOn, true);
            EnsureComp<ActiveSolutionHeaterComponent>(uid);
        }

        public bool TryTurnOn(EntityUid uid, ItemPlacerComponent? placer = null)
        {
            if (!Resolve(uid, ref placer))
                return false;

            if (placer.PlacedEntities.Count <= 0 || !_powerReceiver.IsPowered(uid))
                return false;

            TurnOn(uid);
            return true;
        }

        public void TurnOff(EntityUid uid)
        {
            _appearance.SetData(uid, SolutionHeaterVisuals.IsOn, false);
            RemComp<ActiveSolutionHeaterComponent>(uid);
        }

        private void OnPowerChanged(Entity<SolutionHeaterComponent> entity, ref PowerChangedEvent args)
        {
            var placer = Comp<ItemPlacerComponent>(entity);
            if (args.Powered && placer.PlacedEntities.Count > 0)
            {
                TurnOn(entity);
            }
            else
            {
                TurnOff(entity);
            }
        }

        private void OnItemPlaced(Entity<SolutionHeaterComponent> entity, ref ItemPlacedEvent args)
        {
            TryTurnOn(entity);
        }

        private void OnItemRemoved(Entity<SolutionHeaterComponent> entity, ref ItemRemovedEvent args)
        {
            var placer = Comp<ItemPlacerComponent>(entity);
            if (placer.PlacedEntities.Count == 0) // Last entity was removed
                TurnOff(entity);
        }

        private void OnHandPickUp(EntityUid uid, SolutionHeaterComponent solutionHeaterComponent, ref GotEquippedHandEvent args)
        {
            // Make sure we are dealing with the correct event and user
            if (args.User == null)
            {
                return;
            }

            Log.Info($"Item with SolutionHeaterComponent picked up by {args.User}");

            // Check if the user has an ActiveSolutionHeaterComponent
            if (!HasComp<ActiveSolutionHeaterComponent>(args.User))
            {
                return; // User doesn't have an active heater component
            }

            // Get the user's hand (if they have one)
            if (!TryComp<HandsComponent>(args.User, out var handsComponent))
            {
                return; // User doesn't have a hands component
            }

            // Get the item the user is holding in their active hand
            EntityUid? heldItem = handsComponent.ActiveHand?.HeldEntity;
            if (heldItem == null)
            {
                return; // No item is held in the user's active hand
            }

            // Check if the held item has a SolutionContainerManagerComponent
            if (!TryComp<SolutionContainerManagerComponent>(heldItem.Value, out var container))
            {
                return; // No SolutionContainerManagerComponent on the held item
            }

            // Iterate through all solutions in the container
            foreach (var solutionEntry in _solutionContainer.EnumerateSolutions((heldItem.Value, container)))
            {
                var soln = solutionEntry.Solution;  // Access the solution
                Log.Info($"Heating solution: {soln}");

                // Add thermal energy to the solution
                var energy = solutionHeaterComponent.HeatPerSecond;  // Use the heater component attached to the item
                _solutionContainer.AddThermalEnergy(soln, energy);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ActiveSolutionHeaterComponent, SolutionHeaterComponent, ItemPlacerComponent>();
            while (query.MoveNext(out _, out _, out var heater, out var placer))
            {
                foreach (var heatingEntity in placer.PlacedEntities)
                {
                    if (!TryComp<SolutionContainerManagerComponent>(heatingEntity, out var container))
                        continue;

                    var energy = heater.HeatPerSecond * frameTime;
                    foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((heatingEntity, container)))
                    {
                        _solutionContainer.AddThermalEnergy(soln, energy);
                    }
                }
            }
        }
    }
}

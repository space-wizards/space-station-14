using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Placeable;
using Content.Shared.Hands;
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

        public override void Update(float frameTime)
{
    base.Update(frameTime);

    // Define energy variable that will be reused
    float energy;

    // First, check for placed entities and heat their solutions.
    var query = EntityQueryEnumerator<ActiveSolutionHeaterComponent, SolutionHeaterComponent, ItemPlacerComponent>();
    while (query.MoveNext(out _, out _, out var heater, out var placer))
    {
        foreach (var heatingEntity in placer.PlacedEntities)
        {
            // Only heat solutions in containers that have the necessary component
            if (!TryComp<SolutionContainerManagerComponent>(heatingEntity, out var container))
                continue;

            // Calculate energy based on the heater's heat rate
            energy = heater.HeatPerSecond * frameTime;

            // Apply thermal energy to the solutions in the container
            foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((heatingEntity, container)))
            {
                _solutionContainer.AddThermalEnergy(soln, energy);
            }
        }
    }

    // Now check for players holding valid items that need heating
var playerQuery = EntityQueryEnumerator<HandsComponent>();
while (playerQuery.MoveNext(out var playerUid, out var handsComponent))
{

    if (!HasComp<JellidComponent>(playerUid))
    {
        continue;
    }

    if (handsComponent.ActiveHand?.HeldEntity is not EntityUid heldItem)
    {
        continue;
    }

    if (!TryComp<SolutionContainerManagerComponent>(heldItem, out var container))
    {
        continue;
    }

    float energy2 = 0f;
    energy2 = 30f * frameTime; // God, forgive me for my hardcodedness

    foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((heldItem, container)))
    {
        _solutionContainer.AddThermalEnergy(soln, energy2);
    }
}

}


    }
}

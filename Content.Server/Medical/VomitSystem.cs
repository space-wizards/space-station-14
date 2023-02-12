using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Audio;
using Content.Shared.IdentityManagement;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Medical
{
    public sealed class VomitSystem : EntitySystem
    {

        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly ThirstSystem _thirstSystem = default!;

        /// <summary>
        /// Make an entity vomit, if they have a stomach.
        /// </summary>
        public void Vomit(EntityUid uid, float thirstAdded = -40f, float hungerAdded = -40f)
        {
            // Main requirement: You have a stomach
            var stomachList = _bodySystem.GetBodyOrganComponents<StomachComponent>(uid);
            if (stomachList.Count == 0)
            {
                return;
            }
            // Vomiting makes you hungrier and thirstier
            if (TryComp<HungerComponent>(uid, out var hunger))
                hunger.UpdateFood(hungerAdded);

            if (TryComp<ThirstComponent>(uid, out var thirst))
                _thirstSystem.UpdateThirst(thirst, thirstAdded);

            // It fully empties the stomach, this amount from the chem stream is relatively small
            float solutionSize = (Math.Abs(thirstAdded) + Math.Abs(hungerAdded)) / 6;
            // Apply a bit of slowdown
            if (TryComp<StatusEffectsComponent>(uid, out var status))
                _stunSystem.TrySlowdown(uid, TimeSpan.FromSeconds(solutionSize), true, 0.5f, 0.5f, status);

            var puddle = EntityManager.SpawnEntity("PuddleVomit", Transform(uid).Coordinates);

            var puddleComp = Comp<PuddleComponent>(puddle);

            SoundSystem.Play("/Audio/Effects/Fluids/splat.ogg", Filter.Pvs(uid), uid, AudioHelpers.WithVariation(0.2f).WithVolume(-4f));

            _popupSystem.PopupEntity(Loc.GetString("disease-vomit", ("person", Identity.Entity(uid, EntityManager))), uid);
            // Get the solution of the puddle we spawned
            if (!_solutionSystem.TryGetSolution(puddle, puddleComp.SolutionName, out var puddleSolution))
                return;
            // Empty the stomach out into it
            foreach (var stomach in stomachList)
            {
                if (_solutionSystem.TryGetSolution(stomach.Comp.Owner, StomachSystem.DefaultSolutionName, out var sol))
                    _solutionSystem.TryAddSolution(puddle, puddleSolution, sol);
            }
            // And the small bit of the chem stream from earlier
            if (TryComp<BloodstreamComponent>(uid, out var bloodStream))
            {
                var temp = bloodStream.ChemicalSolution.SplitSolution(solutionSize);
                _solutionSystem.TryAddSolution(puddle, puddleSolution, temp);
            }
        }
    }
}

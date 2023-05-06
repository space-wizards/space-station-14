using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Server.Forensics;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.StatusEffect;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;

namespace Content.Server.Medical
{
    public sealed class VomitSystem : EntitySystem
    {
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly BodySystem _body = default!;
        [Dependency] private readonly HungerSystem _hunger = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
        [Dependency] private readonly StunSystem _stun = default!;
        [Dependency] private readonly ThirstSystem _thirst = default!;

        /// <summary>
        /// Make an entity vomit, if they have a stomach.
        /// </summary>
        public void Vomit(EntityUid uid, float thirstAdded = -40f, float hungerAdded = -40f)
        {
            // Main requirement: You have a stomach
            var stomachList = _body.GetBodyOrganComponents<StomachComponent>(uid);
            if (stomachList.Count == 0)
                return;

            // Vomiting makes you hungrier and thirstier
            if (TryComp<HungerComponent>(uid, out var hunger))
                _hunger.ModifyHunger(uid, hungerAdded, hunger);

            if (TryComp<ThirstComponent>(uid, out var thirst))
                _thirst.UpdateThirst(thirst, thirstAdded);

            // It fully empties the stomach, this amount from the chem stream is relatively small
            var solutionSize = (MathF.Abs(thirstAdded) + MathF.Abs(hungerAdded)) / 6;
            // Apply a bit of slowdown
            if (TryComp<StatusEffectsComponent>(uid, out var status))
                _stun.TrySlowdown(uid, TimeSpan.FromSeconds(solutionSize), true, 0.5f, 0.5f, status);

            var puddle = EntityManager.SpawnEntity("PuddleVomit", Transform(uid).Coordinates);

            var forensics = EnsureComp<ForensicsComponent>(puddle);
            if (TryComp<DnaComponent>(uid, out var dna))
                forensics.DNAs.Add(dna.DNA);

            var puddleComp = Comp<PuddleComponent>(puddle);

            _audio.PlayPvs("/Audio/Effects/Fluids/splat.ogg", uid, AudioParams.Default.WithVariation(0.2f).WithVolume(-4f));

            _popup.PopupEntity(Loc.GetString("disease-vomit", ("person", Identity.Entity(uid, EntityManager))), uid);
            // Get the solution of the puddle we spawned
            if (!_solutionContainer.TryGetSolution(puddle, puddleComp.SolutionName, out var puddleSolution))
                return;
            // Empty the stomach out into it
            foreach (var stomach in stomachList)
            {
                if (_solutionContainer.TryGetSolution(stomach.Comp.Owner, StomachSystem.DefaultSolutionName, out var sol))
                    _solutionContainer.TryAddSolution(puddle, puddleSolution, sol);
            }
            // And the small bit of the chem stream from earlier
            if (TryComp<BloodstreamComponent>(uid, out var bloodStream))
            {
                var temp = bloodStream.ChemicalSolution.SplitSolution(solutionSize);
                _solutionContainer.TryAddSolution(puddle, puddleSolution, temp);
            }
        }
    }
}

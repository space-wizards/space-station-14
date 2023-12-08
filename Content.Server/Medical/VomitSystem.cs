using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Forensics;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.StatusEffect;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical
{
    public sealed class VomitSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly BodySystem _body = default!;
        [Dependency] private readonly HungerSystem _hunger = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PuddleSystem _puddle = default!;
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
                _thirst.ModifyThirst(uid, thirst, thirstAdded);

            // It fully empties the stomach, this amount from the chem stream is relatively small
            var solutionSize = (MathF.Abs(thirstAdded) + MathF.Abs(hungerAdded)) / 6;
            // Apply a bit of slowdown
            if (TryComp<StatusEffectsComponent>(uid, out var status))
                _stun.TrySlowdown(uid, TimeSpan.FromSeconds(solutionSize), true, 0.5f, 0.5f, status);

            // TODO: Need decals
            var solution = new Solution();

            // Empty the stomach out into it
            foreach (var stomach in stomachList)
            {
                if (_solutionContainer.TryGetSolution(stomach.Comp.Owner, StomachSystem.DefaultSolutionName,
                        out var sol))
                {
                    solution.AddSolution(sol, _proto);
                    sol.RemoveAllSolution();
                    _solutionContainer.UpdateChemicals(stomach.Comp.Owner, sol);
                }
            }
            // Adds a tiny amount of the chem stream from earlier along with vomit
            if (TryComp<BloodstreamComponent>(uid, out var bloodStream))
            {
                var chemMultiplier = 0.1;
                var vomitMultiplier = 0.9;

                // Makes a vomit solution the size of 90% of the chemicals removed from the chemstream
                var vomitAmount = new Solution("Vomit", solutionSize * vomitMultiplier);

                // Takes 10% of the chemicals removed from the chem stream
                var vomitChemstreamAmount = _solutionContainer.SplitSolution(uid, bloodStream.ChemicalSolution, solutionSize * chemMultiplier);

                _solutionContainer.SplitSolution(uid, bloodStream.ChemicalSolution, solutionSize * vomitMultiplier);
                solution.AddSolution(vomitAmount, _proto);
                solution.AddSolution(vomitChemstreamAmount, _proto);
            }

            if (_puddle.TrySpillAt(uid, solution, out var puddle, false))
            {
                var forensics = EnsureComp<ForensicsComponent>(puddle);
                if (TryComp<DnaComponent>(uid, out var dna))
                    forensics.DNAs.Add(dna.DNA);
            }

            // Force sound to play as spill doesn't work if solution is empty.
            _audio.PlayPvs("/Audio/Effects/Fluids/splat.ogg", uid, AudioParams.Default.WithVariation(0.2f).WithVolume(-4f));
            _popup.PopupEntity(Loc.GetString("disease-vomit", ("person", Identity.Entity(uid, EntityManager))), uid);
        }
    }
}

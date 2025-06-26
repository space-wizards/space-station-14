using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Forensics;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.StatusEffect;
using Robust.Server.Audio;
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
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
        [Dependency] private readonly StunSystem _stun = default!;
        [Dependency] private readonly ThirstSystem _thirst = default!;
        [Dependency] private readonly ForensicsSystem _forensics = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;

        [ValidatePrototypeId<SoundCollectionPrototype>]
        private const string VomitCollection = "Vomit";

        private readonly SoundSpecifier _vomitSound = new SoundCollectionSpecifier(VomitCollection,
            AudioParams.Default.WithVariation(0.2f).WithVolume(-4f));

        /// <summary>
        /// Make an entity vomit, if they have a stomach.
        /// </summary>
        public void Vomit(EntityUid uid, float thirstAdded = -40f, float hungerAdded = -40f)
        {
            // Main requirement: You have a stomach
            var stomachList = _body.GetBodyOrganEntityComps<StomachComponent>(uid);
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
                if (_solutionContainer.ResolveSolution(stomach.Owner, StomachSystem.DefaultSolutionName, ref stomach.Comp1.Solution, out var sol))
                {
                    solution.AddSolution(sol, _proto);
                    sol.RemoveAllSolution();
                    _solutionContainer.UpdateChemicals(stomach.Comp1.Solution.Value);
                }
            }
            // Adds a tiny amount of the chem stream from earlier along with vomit
            if (TryComp<BloodstreamComponent>(uid, out var bloodStream))
            {
                const float chemMultiplier = 0.1f;

                var vomitAmount = solutionSize;

                // Takes 10% of the chemicals removed from the chem stream
                if (_solutionContainer.ResolveSolution(uid, bloodStream.ChemicalSolutionName, ref bloodStream.ChemicalSolution))
                {
                    var vomitChemstreamAmount = _solutionContainer.SplitSolution(bloodStream.ChemicalSolution.Value, vomitAmount);
                    vomitChemstreamAmount.ScaleSolution(chemMultiplier);
                    solution.AddSolution(vomitChemstreamAmount, _proto);

                    vomitAmount -= (float)vomitChemstreamAmount.Volume;
                }

                // Makes a vomit solution the size of 90% of the chemicals removed from the chemstream
                solution.AddReagent(new ReagentId("Vomit", _bloodstream.GetEntityBloodData(uid)), vomitAmount); // TODO: Dehardcode vomit prototype
            }

            if (_puddle.TrySpillAt(uid, solution, out var puddle, false))
            {
                _forensics.TransferDna(puddle, uid, false);
            }

            // Force sound to play as spill doesn't work if solution is empty.
            _audio.PlayPvs(_vomitSound, uid);
            _popup.PopupEntity(Loc.GetString("disease-vomit", ("person", Identity.Entity(uid, EntityManager))), uid);
        }
    }
}

using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Fluids;
using Content.Shared.Forensics.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical;

public sealed class VomitSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedForensicsSystem _forensics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, TryVomitEvent>(TryBodyVomitSolution);
    }

    private const float ChemMultiplier = 0.1f;

    private static readonly ProtoId<SoundCollectionPrototype> VomitCollection = "Vomit";

    private static readonly ProtoId<ReagentPrototype> VomitPrototype = "Vomit";  // TODO: Dehardcode vomit prototype

    private readonly SoundSpecifier _vomitSound = new SoundCollectionSpecifier(VomitCollection,
        AudioParams.Default.WithVariation(0.2f).WithVolume(-4f));

    private void TryBodyVomitSolution(Entity<BodyComponent> ent, ref TryVomitEvent args)
    {
        if (args.Handled)
            return;

        // Main requirement: You have a stomach
        var stomachList = _body.GetBodyOrganEntityComps<StomachComponent>((ent, null));
        if (stomachList.Count == 0)
            return;

        // Empty the stomach out into it
        foreach (var stomach in stomachList)
        {
            if (_solutionContainer.ResolveSolution(stomach.Owner, StomachSystem.DefaultSolutionName, ref stomach.Comp1.Solution, out var sol))
                _solutionContainer.TryTransferSolution(stomach.Comp1.Solution.Value, args.Sol, sol.AvailableVolume);
        }

        args.Handled = true;
    }

    /// <summary>
    /// Make an entity vomit, if they have a stomach.
    /// </summary>
    public void Vomit(EntityUid uid, float thirstAdded = -40f, float hungerAdded = -40f, bool force = false)
    {
        // Vomit only if entity is alive
        // Ignore condition if force was set to true
        if (!force && _mobState.IsDead(uid))
            return;

        // TODO: Need decals
        var solution = new Solution();

        var ev = new TryVomitEvent(solution, force);
        RaiseLocalEvent(uid, ref ev);

        if (!ev.Handled)
            return;

        // Vomiting makes you hungrier and thirstier
        if (TryComp<HungerComponent>(uid, out var hunger))
            _hunger.ModifyHunger(uid, hungerAdded, hunger);

        if (TryComp<ThirstComponent>(uid, out var thirst))
            _thirst.ModifyThirst(uid, thirst, thirstAdded);

        // It fully empties the stomach, this amount from the chem stream is relatively small
        var solutionSize = (MathF.Abs(thirstAdded) + MathF.Abs(hungerAdded)) / 6;

        // Apply a bit of slowdown
        _movementMod.TryUpdateMovementSpeedModDuration(uid, MovementModStatusSystem.VomitingSlowdown, TimeSpan.FromSeconds(solutionSize), 0.5f);

        // Adds a tiny amount of the chem stream from earlier along with vomit
        if (TryComp<BloodstreamComponent>(uid, out var bloodStream))
        {
            var vomitAmount = solutionSize;

            // Flushes small portion of the chemicals removed from the bloodstream stream
            if (_solutionContainer.ResolveSolution(uid, bloodStream.BloodSolutionName, ref bloodStream.BloodSolution))
            {
                var vomitChemstreamAmount = _bloodstream.FlushChemicals((uid, bloodStream), vomitAmount);

                if (vomitChemstreamAmount != null)
                {
                    vomitChemstreamAmount.ScaleSolution(ChemMultiplier);
                    solution.AddSolution(vomitChemstreamAmount, _proto);
                    vomitAmount -= (float)vomitChemstreamAmount.Volume;
                }
            }

            // Makes a vomit solution the size of 90% of the chemicals removed from the chemstream
            solution.AddReagent(new ReagentId(VomitPrototype, _bloodstream.GetEntityBloodData((uid, bloodStream))), vomitAmount);
        }

        if (_puddle.TrySpillAt(uid, solution, out var puddle, false))
        {
            _forensics.TransferDna(puddle, uid, false);
        }


        if (!_netManager.IsServer)
            return;

        // Force sound to play as spill doesn't work if solution is empty.
        _audio.PlayPvs(_vomitSound, uid);
        _popup.PopupEntity(Loc.GetString("disease-vomit", ("person", Identity.Entity(uid, EntityManager))), uid);
    }
}

[ByRefEvent]
public record struct TryVomitEvent(Solution Sol, bool Forced = false, bool Handled = false);

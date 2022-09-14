using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Server.Kitchen.Components;
using Content.Shared.Interaction;
using Content.Shared.Examine;
using Content.Shared.Tag;
using Content.Shared.FixedPoint;
using Content.Shared.Audio;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems
{
    public sealed class PlantHolderSystem : EntitySystem
    {
        [Dependency] private readonly BotanySystem _botanySystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PlantHolderComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<PlantHolderComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<PlantHolderComponent, InteractHandEvent>(OnInteractHand);
        }

        private void OnExamine(EntityUid uid, PlantHolderComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            if (component.Seed == null)
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-nothing-planted-message"));
            }
            else if (!component.Dead)
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-something-already-growing-message",
                                      ("seedName", component.Seed.DisplayName),
                                      ("toBeForm", component.Seed.DisplayName.EndsWith('s') ? "are" : "is")));

                if (component.Health <= component.Seed.Endurance / 2)
                    args.PushMarkup(Loc.GetString(
                                          "plant-holder-component-something-already-growing-low-health-message",
                                          ("healthState",
                                              Loc.GetString(component.Age > component.Seed.Lifespan
                                                  ? "plant-holder-component-plant-old-adjective"
                                                  : "plant-holder-component-plant-unhealthy-adjective"))));
            }
            else
            {
                args.PushMarkup(Loc.GetString("plant-holder-component-dead-plant-matter-message"));
            }

            if (component.WeedLevel >= 5)
                args.PushMarkup(Loc.GetString("plant-holder-component-weed-high-level-message"));

            if (component.PestLevel >= 5)
                args.PushMarkup(Loc.GetString("plant-holder-component-pest-high-level-message"));

            args.PushMarkup(Loc.GetString($"plant-holder-component-water-level-message",
                ("waterLevel", (int) component.WaterLevel)));
            args.PushMarkup(Loc.GetString($"plant-holder-component-nutrient-level-message",
                ("nutritionLevel", (int) component.NutritionLevel)));

            if (component.DrawWarnings)
            {
                if (component.Toxins > 40f)
                    args.PushMarkup(Loc.GetString("plant-holder-component-toxins-high-warning"));

                if (component.ImproperLight)
                    args.PushMarkup(Loc.GetString("plant-holder-component-light-improper-warning"));

                if (component.ImproperHeat)
                    args.PushMarkup(Loc.GetString("plant-holder-component-heat-improper-warning"));

                if (component.ImproperPressure)
                    args.PushMarkup(Loc.GetString("plant-holder-component-pressure-improper-warning"));

                if (component.MissingGas > 0)
                    args.PushMarkup(Loc.GetString("plant-holder-component-gas-missing-warning"));
            }
        }

        private void OnInteractUsing(EntityUid uid, PlantHolderComponent component, InteractUsingEvent args)
        {
            if (TryComp(args.Used, out SeedComponent? seeds))
            {
                if (component.Seed == null)
                {
                    if (!_botanySystem.TryGetSeed(seeds, out var seed))
                        return ;

                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-plant-success-message",
                        ("seedName", seed.Name),
                        ("seedNoun", seed.Noun)), Filter.Entities(args.User), PopupType.Medium);

                    component.Seed = seed;
                    component.Dead = false;
                    component.Age = 1;
                    component.Health = component.Seed.Endurance;
                    component.LastCycle = _gameTiming.CurTime;

                    EntityManager.QueueDeleteEntity(args.Used);

                    component.CheckLevelSanity();
                    component.UpdateSprite();

                    return;
                }

                _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-already-seeded-message",
                    ("name", Comp<MetaDataComponent>(uid).EntityName)), Filter.Entities(args.User), PopupType.Medium);
                return;
            }

            if (_tagSystem.HasTag(args.Used, "Hoe"))
            {
                if (component.WeedLevel > 0)
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-remove-weeds-message",
                        ("name", Comp<MetaDataComponent>(uid).EntityName)), Filter.Entities(args.User), PopupType.Medium);
                    _popupSystem.PopupEntity(Loc.GetString("plant-holder-component-remove-weeds-others-message",
                        ("otherName", Comp<MetaDataComponent>(args.User).EntityName)), uid, Filter.PvsExcept(args.User));
                    component.WeedLevel = 0;
                    component.UpdateSprite();
                }
                else
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-no-weeds-message"), Filter.Entities(args.User));
                }

                return;
            }

            if (_tagSystem.HasTag(args.Used, "Shovel"))
            {
                if (component.Seed != null)
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-remove-plant-message",
                        ("name", Comp<MetaDataComponent>(uid).EntityName)), Filter.Entities(args.User), PopupType.Medium);
                    _popupSystem.PopupEntity(Loc.GetString("plant-holder-component-remove-plant-others-message",
                        ("name", Comp<MetaDataComponent>(args.User).EntityName)), uid, Filter.PvsExcept(args.User));
                    component.RemovePlant();
                }
                else
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-no-plant-message",
                        ("name", Comp<MetaDataComponent>(uid).EntityName)), Filter.Entities(args.User));
                }

                return;
            }

            if (_solutionSystem.TryGetDrainableSolution(args.Used, out var solution)
                && _solutionSystem.TryGetSolution(uid, component.SoilSolutionName, out var targetSolution) && TryComp(args.Used, out SprayComponent? spray))
            {
                var amount = FixedPoint2.New(1);

                var targetEntity = uid;
                var solutionEntity = args.Used;


                SoundSystem.Play(spray.SpraySound.GetSound(), Filter.Pvs(args.Used),
                args.Used, AudioHelpers.WithVariation(0.125f));


                var split =_solutionSystem.Drain(solutionEntity, solution, amount);

                if (split.TotalVolume == 0)
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-no-plant-message",
                        ("owner", args.Used)), Filter.Entities(args.User));
                    return;
                }

                _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-spray-message",
                    ("owner", uid),
                    ("amount", split.TotalVolume)), Filter.Entities(args.User), PopupType.Medium);

               _solutionSystem.TryAddSolution(targetEntity, targetSolution, split);

                component.ForceUpdateByExternalCause();

                return;
            }

            if (_tagSystem.HasTag(args.Used, "PlantSampleTaker"))
            {
                if (component.Seed == null)
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-nothing-to-sample-message"), Filter.Entities(args.User));
                    return;
                }

                if (component.Sampled)
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-already-sampled-message"), Filter.Entities(args.User));
                    return;
                }

                if (component.Dead)
                {
                    _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-dead-plant-message"), Filter.Entities(args.User));
                    return;
                }

                component.Seed.Unique = false;
                var seed = _botanySystem.SpawnSeedPacket(component.Seed, Transform(args.User).Coordinates);
                seed.RandomOffset(0.25f);
                _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-take-sample-message",
                    ("seedName", component.Seed.DisplayName)), Filter.Entities(args.User));
                component.Health -= (_random.Next(3, 5) * 10);

                if (_random.Prob(0.3f))
                    component.Sampled = true;

                // Just in case.
                component.CheckLevelSanity();
                component.ForceUpdateByExternalCause();

                return;
            }

            if (HasComp<SharpComponent>(args.Used))
                component.DoHarvest(args.User);

            if (TryComp<ProduceComponent?>(args.Used, out var produce))
            {
                _popupSystem.PopupCursor(Loc.GetString("plant-holder-component-compost-message",
                    ("owner", uid),
                    ("usingItem", args.Used)), Filter.Entities(args.User), PopupType.Medium);
                _popupSystem.PopupEntity(Loc.GetString("plant-holder-component-compost-others-message",
                    ("user", Identity.Entity(args.User, EntityManager)),
                    ("usingItem", args.Used),
                    ("owner", uid)), uid, Filter.PvsExcept(args.User));

                if (_solutionSystem.TryGetSolution(args.Used, produce.SolutionName, out var solution2))
                {
                    // This deliberately discards overfill.
                   _solutionSystem.TryAddSolution(args.Used, solution2,
                       _solutionSystem.SplitSolution(args.Used, solution2, solution2.TotalVolume));

                    component.ForceUpdateByExternalCause();
                }

                EntityManager.QueueDeleteEntity(args.Used);
            }
        }

        private void OnInteractHand(EntityUid uid, PlantHolderComponent component, InteractHandEvent args)
        {
            component.DoHarvest(args.User);
        }
    }
}

using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Server.Forensics;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using System.Linq;
using Content.Shared.Atmos;

namespace Content.Server.Nutrition.EntitySystems
{
    public sealed partial class SmokingSystem : EntitySystem
    {
        [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmos = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly ClothingSystem _clothing = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedItemSystem _items = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly ForensicsSystem _forensics = default!;

        private const float UpdateTimer = 3f;

        private float _timer;

        public override void Initialize()
        {
            SubscribeLocalEvent<SmokableComponent, IsHotEvent>(OnSmokableIsHotEvent);
            SubscribeLocalEvent<SmokableComponent, ComponentShutdown>(OnSmokableShutdownEvent);
            SubscribeLocalEvent<SmokableComponent, GotEquippedEvent>(OnSmokeableEquipEvent);
            Subs.SubscribeWithRelay<SmokableComponent, ExtinguishEvent>(OnExtinguishEvent);

            InitializeCigars();
            InitializePipes();
            InitializeVapes();
        }

        private void OnExtinguishEvent(Entity<SmokableComponent> ent, ref ExtinguishEvent args)
        {
            if (ent.Comp.State == SmokableState.Lit)
                SetSmokableState(ent, SmokableState.Burnt, ent);
        }

        public void SetSmokableState(EntityUid uid, SmokableState state, SmokableComponent? smokable = null,
            AppearanceComponent? appearance = null, ClothingComponent? clothing = null)
        {
            if (!Resolve(uid, ref smokable, ref appearance, ref clothing) || smokable.State == state)
                return;

            smokable.State = state;
            _appearance.SetData(uid, SmokingVisuals.Smoking, state, appearance);

            var newState = state switch
            {
                SmokableState.Lit => smokable.LitPrefix,
                SmokableState.Burnt => smokable.BurntPrefix,
                _ => smokable.UnlitPrefix
            };

            _clothing.SetEquippedPrefix(uid, newState, clothing);
            _items.SetHeldPrefix(uid, newState);

            if (state == SmokableState.Lit)
            {
                EnsureComp<BurningComponent>(uid);
                _audio.PlayPvs(smokable.LightSound, uid);
                var igniteEvent = new IgnitedEvent();
                RaiseLocalEvent(uid, ref igniteEvent);
            }
            else
            {
                RemComp<BurningComponent>(uid);
                _audio.PlayPvs(smokable.SnuffSound, uid);
                var extinguishEvent = new ExtinguishedEvent();
                RaiseLocalEvent(uid, ref extinguishEvent);
            }
        }

        private void OnSmokableIsHotEvent(Entity<SmokableComponent> entity, ref IsHotEvent args)
        {
            args.IsHot = entity.Comp.State == SmokableState.Lit;
        }

        private void OnSmokableShutdownEvent(Entity<SmokableComponent> entity, ref ComponentShutdown args)
        {
            RemComp<BurningComponent>(entity);
        }

        private void OnSmokeableEquipEvent(Entity<SmokableComponent> entity, ref GotEquippedEvent args)
        {
            if (args.Slot == "mask")
            {
                _forensics.TransferDna(entity.Owner, args.Equipee, false);
            }
        }

        public override void Update(float frameTime)
        {
            _timer += frameTime;

            if (_timer < UpdateTimer)
                return;

            var query = EntityQueryEnumerator<BurningComponent, SmokableComponent>();
            while (query.MoveNext(out var uid, out _, out var smokable))
            {
                if (!_solutionContainerSystem.TryGetSolution(uid, smokable.Solution, out var soln, out var solution))
                {
                    SetSmokableState(uid, SmokableState.Unlit, smokable);
                    continue;
                }

                if (smokable.ExposeTemperature > 0 && smokable.ExposeVolume > 0)
                {
                    var transform = Transform(uid);

                    if (transform.GridUid is { } gridUid)
                    {
                        var position = _transformSystem.GetGridOrMapTilePosition(uid, transform);
                        _atmos.HotspotExpose(gridUid, position, smokable.ExposeTemperature, smokable.ExposeVolume, uid, true);
                    }
                }

                var inhaledSolution = _solutionContainerSystem.SplitSolution(soln.Value, smokable.InhaleAmount * _timer);

                if (solution.Volume == FixedPoint2.Zero)
                {
                    RaiseLocalEvent(uid, new SmokableSolutionEmptyEvent(), true);
                }

                if (inhaledSolution.Volume == FixedPoint2.Zero)
                    continue;

                // This is awful. I hate this so much.
                // TODO: Please, someone refactor containers and free me from this bullshit.
                if (!_container.TryGetContainingContainer((uid, null, null), out var containerManager) ||
                    !(_inventorySystem.TryGetSlotEntity(containerManager.Owner, "mask", out var inMaskSlotUid) && inMaskSlotUid == uid) ||
                    !TryComp(containerManager.Owner, out BloodstreamComponent? bloodstream))
                {
                    continue;
                }

                _reactiveSystem.DoEntityReaction(containerManager.Owner, inhaledSolution, ReactionMethod.Ingestion);
                _bloodstreamSystem.TryAddToChemicals(containerManager.Owner, inhaledSolution, bloodstream);
            }

            _timer -= UpdateTimer;
        }
    }

    /// <summary>
    ///     Directed event raised when the smokable solution is empty.
    /// </summary>
    public sealed class SmokableSolutionEmptyEvent : EntityEventArgs
    {
    }
}

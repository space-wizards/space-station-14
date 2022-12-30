using Content.Server.Administration.Logs;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.NodeGroups;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.Electrocution;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Jittering;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Pulling.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Electrocution
{
    public sealed class ElectrocutionSystem : SharedElectrocutionSystem
    {
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly SharedJitteringSystem _jitteringSystem = default!;
        [Dependency] private readonly SharedStunSystem _stunSystem = default!;
        [Dependency] private readonly SharedStutteringSystem _stutteringSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly NodeGroupSystem _nodeGroupSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger= default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;

        private const string StatusEffectKey = "Electrocution";
        private const string DamageType = "Shock";

        // Yes, this is absurdly small for a reason.
        private const float ElectrifiedDamagePerWatt = 0.0015f;

        private const float RecursiveDamageMultiplier = 0.75f;
        private const float RecursiveTimeMultiplier = 0.8f;

        private const float ParalyzeTimeMultiplier = 1f;

        private const float StutteringTimeMultiplier = 1.5f;

        private const float JitterTimeMultiplier = 0.75f;
        private const float JitterAmplitude = 80f;
        private const float JitterFrequency = 8f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ElectrifiedComponent, StartCollideEvent>(OnElectrifiedStartCollide);
            SubscribeLocalEvent<ElectrifiedComponent, AttackedEvent>(OnElectrifiedAttacked);
            SubscribeLocalEvent<ElectrifiedComponent, InteractHandEvent>(OnElectrifiedHandInteract);
            SubscribeLocalEvent<ElectrifiedComponent, InteractUsingEvent>(OnElectrifiedInteractUsing);
            SubscribeLocalEvent<RandomInsulationComponent, MapInitEvent>(OnRandomInsulationMapInit);

            UpdatesAfter.Add(typeof(PowerNetSystem));
        }

        public override void Update(float frameTime)
        {
            // Update "in progress" electrocutions

            RemQueue<ElectrocutionComponent> finishedElectrocutionsQueue = new();
            foreach (var (electrocution, consumer) in EntityManager
                .EntityQuery<ElectrocutionComponent, PowerConsumerComponent>())
            {
                var ftAdjusted = Math.Min(frameTime, electrocution.TimeLeft);

                electrocution.TimeLeft -= ftAdjusted;
                electrocution.AccumulatedDamage += consumer.ReceivedPower * ElectrifiedDamagePerWatt * ftAdjusted;

                if (MathHelper.CloseTo(electrocution.TimeLeft, 0))
                    finishedElectrocutionsQueue.Add(electrocution);
            }

            foreach (var finished in finishedElectrocutionsQueue)
            {
                var uid = finished.Owner;
                if (EntityManager.EntityExists(finished.Electrocuting))
                {
                    // TODO: damage should be scaled by shock damage multiplier
                    // TODO: better paralyze/jitter timing
                    var damage = new DamageSpecifier(
                        _prototypeManager.Index<DamageTypePrototype>(DamageType),
                        (int) finished.AccumulatedDamage);

                    var actual = _damageableSystem.TryChangeDamage(finished.Electrocuting, damage, origin: finished.Source);
                    if (actual != null)
                    {
                        _adminLogger.Add(LogType.Electrocution,
                            $"{ToPrettyString(finished.Electrocuting):entity} received {actual.Total:damage} powered electrocution damage from {ToPrettyString(finished.Source):source}");
                    }
                }

                EntityManager.DeleteEntity(uid);
            }
        }

        private void OnElectrifiedStartCollide(EntityUid uid, ElectrifiedComponent electrified, ref StartCollideEvent args)
        {
            if (!electrified.OnBump)
                return;

            TryDoElectrifiedAct(uid, args.OtherFixture.Body.Owner, 1, electrified);
        }

        private void OnElectrifiedAttacked(EntityUid uid, ElectrifiedComponent electrified, AttackedEvent args)
        {
            if (!electrified.OnAttacked)
                return;

            TryDoElectrifiedAct(uid, args.User, 1, electrified);
        }

        private void OnElectrifiedHandInteract(EntityUid uid, ElectrifiedComponent electrified, InteractHandEvent args)
        {
            if (!electrified.OnHandInteract)
                return;

            TryDoElectrifiedAct(uid, args.User, 1, electrified);
        }

        private void OnElectrifiedInteractUsing(EntityUid uid, ElectrifiedComponent electrified, InteractUsingEvent args)
        {
            if (!electrified.OnInteractUsing)
                return;

            var siemens = TryComp(args.Used, out InsulatedComponent? insulation)
                ? insulation.SiemensCoefficient
                : 1;

            TryDoElectrifiedAct(uid, args.User, siemens, electrified);
        }

        public bool TryDoElectrifiedAct(EntityUid uid, EntityUid targetUid,
            float siemens = 1,
            ElectrifiedComponent? electrified = null,
            NodeContainerComponent? nodeContainer = null,
            TransformComponent? transform = null)
        {
            if (!Resolve(uid, ref electrified, ref transform, false))
                return false;

            if (!electrified.Enabled)
                return false;

            if (electrified.NoWindowInTile)
            {
                foreach (var entity in transform.Coordinates.GetEntitiesInTile(
                    LookupFlags.Approximate | LookupFlags.Static, _entityLookup))
                {
                    if (_tagSystem.HasTag(entity, "Window"))
                        return false;
                }
            }

            siemens *= electrified.SiemensCoefficient;
            if (!DoCommonElectrocutionAttempt(targetUid, uid, ref siemens) || siemens <= 0)
                return false; // If electrocution would fail, do nothing.

            var targets = new List<(EntityUid entity, int depth)>();
            GetChainedElectrocutionTargets(targetUid, targets);
            if (!electrified.RequirePower || electrified.UsesApcPower)
            {
                // Does it use APC power for its electrification check? Check if it's powered, and then
                // attempt an electrocution if all the checks succeed.

                if (electrified.UsesApcPower && !this.IsPowered(uid, EntityManager))
                {
                    return false;
                }

                var lastRet = true;
                for (var i = targets.Count - 1; i >= 0; i--)
                {
                    var (entity, depth) = targets[i];
                    lastRet = TryDoElectrocution(
                        entity,
                        uid,
                        (int) (electrified.ShockDamage * MathF.Pow(RecursiveDamageMultiplier, depth)),
                        TimeSpan.FromSeconds(electrified.ShockTime * MathF.Pow(RecursiveTimeMultiplier, depth)), true,
                        electrified.SiemensCoefficient);
                }

                return lastRet;
            }

            if (!Resolve(uid, ref nodeContainer, false))
                return false;

            var node = TryNode(electrified.HighVoltageNode) ??
                       TryNode(electrified.MediumVoltageNode) ??
                       TryNode(electrified.LowVoltageNode);

            if (node == null)
                return false;

            var (damageMult, timeMult) = node.NodeGroupID switch
            {
                NodeGroupID.HVPower => (electrified.HighVoltageDamageMultiplier, electrified.HighVoltageTimeMultiplier),
                NodeGroupID.MVPower => (electrified.MediumVoltageDamageMultiplier,
                    electrified.MediumVoltageTimeMultiplier),
                _ => (1f, 1f)
            };

            {
                var lastRet = true;
                for (var i = targets.Count - 1; i >= 0; i--)
                {
                    var (entity, depth) = targets[i];
                    lastRet = TryDoElectrocutionPowered(
                        entity,
                        uid,
                        node,
                        (int) (electrified.ShockDamage * MathF.Pow(RecursiveDamageMultiplier, depth) * damageMult),
                        TimeSpan.FromSeconds(electrified.ShockTime * MathF.Pow(RecursiveTimeMultiplier, depth) *
                                             timeMult), true,
                        electrified.SiemensCoefficient);
                }

                return lastRet;
            }


            Node? TryNode(string? id)
            {
                if (id != null && nodeContainer.TryGetNode<Node>(id, out var tryNode)
                               && tryNode.NodeGroup is IBasePowerNet { NetworkNode: { LastCombinedSupply: >0 } })
                {
                    return tryNode;
                }

                return null;
            }
        }

        /// <param name="uid">Entity being electrocuted.</param>
        /// <param name="sourceUid">Source entity of the electrocution.</param>
        /// <param name="shockDamage">How much shock damage the entity takes.</param>
        /// <param name="time">How long the entity will be stunned.</param>
        /// <param name="refresh">Should <paramref>time</paramref> be refreshed (instead of accumilated) if the entity is already electrocuted?</param>
        /// <param name="siemensCoeffiecient">How insulated the entity is from the shock. 0 means completely insulated, and 1 means no insulation.</param>
        /// <param name="statusEffect">Status effect to apply to the entity.</param>
        /// <param name="ignoreInsulation">Should the electrocution bypass the Insulated component?</param>
        /// <returns>Whether the entity <see cref="uid"/> was stunned by the shock.</returns>
        public bool TryDoElectrocution(
            EntityUid uid, EntityUid? sourceUid, int shockDamage, TimeSpan time, bool refresh, float siemensCoefficient = 1f,
            StatusEffectsComponent? statusEffects = null, bool ignoreInsulation = false)
        {
            if (!DoCommonElectrocutionAttempt(uid, sourceUid, ref siemensCoefficient, ignoreInsulation)
                || !DoCommonElectrocution(uid, sourceUid, shockDamage, time, refresh, siemensCoefficient, statusEffects))
                return false;

            RaiseLocalEvent(uid, new ElectrocutedEvent(uid, sourceUid, siemensCoefficient), true);
            return true;
        }

        private bool TryDoElectrocutionPowered(
            EntityUid uid,
            EntityUid sourceUid,
            Node node,
            int shockDamage,
            TimeSpan time,
            bool refresh,
            float siemensCoefficient = 1f,
            StatusEffectsComponent? statusEffects = null,
            TransformComponent? sourceTransform = null)
        {
            if (!DoCommonElectrocutionAttempt(uid, sourceUid, ref siemensCoefficient))
                return false;

            // Coefficient needs to be higher than this to do a powered electrocution!
            if (siemensCoefficient <= 0.5f)
                return DoCommonElectrocution(uid, sourceUid, shockDamage, time, refresh, siemensCoefficient, statusEffects);

            if (!DoCommonElectrocution(uid, sourceUid, null, time, refresh, siemensCoefficient, statusEffects))
                return false;

            if (!Resolve(sourceUid, ref sourceTransform)) // This shouldn't really happen, but just in case...
                return true;

            var electrocutionEntity = EntityManager.SpawnEntity(
                $"VirtualElectrocutionLoad{node.NodeGroupID}", sourceTransform.Coordinates);

            var electrocutionNode = EntityManager.GetComponent<NodeContainerComponent>(electrocutionEntity)
                .GetNode<ElectrocutionNode>("electrocution");

            var electrocutionComponent = EntityManager.GetComponent<ElectrocutionComponent>(electrocutionEntity);

            electrocutionNode.CableEntity = sourceUid;
            electrocutionNode.NodeName = node.Name;

            _nodeGroupSystem.QueueReflood(electrocutionNode);

            electrocutionComponent.TimeLeft = 1f;
            electrocutionComponent.Electrocuting = uid;
            electrocutionComponent.Source = sourceUid;

            RaiseLocalEvent(uid, new ElectrocutedEvent(uid, sourceUid, siemensCoefficient), true);

            return true;
        }

        private bool DoCommonElectrocutionAttempt(EntityUid uid, EntityUid? sourceUid, ref float siemensCoefficient, bool ignoreInsulation = false)
        {

            var attemptEvent = new ElectrocutionAttemptEvent(uid, sourceUid, siemensCoefficient,
                ignoreInsulation ? SlotFlags.NONE : ~SlotFlags.POCKET);
            RaiseLocalEvent(uid, attemptEvent, true);

            // Cancel the electrocution early, so we don't recursively electrocute anything.
            if (attemptEvent.Cancelled)
                return false;

            siemensCoefficient = attemptEvent.SiemensCoefficient;
            return true;
        }

        private bool DoCommonElectrocution(EntityUid uid, EntityUid? sourceUid,
            int? shockDamage, TimeSpan time, bool refresh, float siemensCoefficient = 1f,
            StatusEffectsComponent? statusEffects = null)
        {
            if (siemensCoefficient <= 0)
                return false;

            if (shockDamage != null)
            {
                shockDamage = (int) (shockDamage * siemensCoefficient);

                if (shockDamage.Value <= 0)
                    return false;
            }

            if (!Resolve(uid, ref statusEffects, false) ||
                !_statusEffectsSystem.CanApplyEffect(uid, StatusEffectKey, statusEffects))
                return false;

            if (!_statusEffectsSystem.TryAddStatusEffect<ElectrocutedComponent>(uid, StatusEffectKey, time, refresh,
                statusEffects))
                return false;

            var shouldStun = siemensCoefficient > 0.5f;

            if (shouldStun)
                _stunSystem.TryParalyze(uid, time * ParalyzeTimeMultiplier, refresh, statusEffects);

            // TODO: Sparks here.

            if(shockDamage is {} dmg)
            {
                var actual = _damageableSystem.TryChangeDamage(uid,
                    new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>(DamageType), dmg), origin: sourceUid);

                if (actual != null)
                {
                    _adminLogger.Add(LogType.Electrocution,
                        $"{ToPrettyString(statusEffects.Owner):entity} received {actual.Total:damage} powered electrocution damage{(sourceUid != null ? " from " + ToPrettyString(sourceUid.Value) : ""):source}");
                }
            }

            _stutteringSystem.DoStutter(uid, time * StutteringTimeMultiplier, refresh, statusEffects);
            _jitteringSystem.DoJitter(uid, time * JitterTimeMultiplier, refresh, JitterAmplitude, JitterFrequency, true,
                statusEffects);

            _popupSystem.PopupEntity(Loc.GetString("electrocuted-component-mob-shocked-popup-player"), uid, uid);

            var filter = Filter.PvsExcept(uid, entityManager: EntityManager);

            // TODO: Allow being able to pass EntityUid to Loc...
            if (sourceUid != null)
            {
                _popupSystem.PopupEntity(Loc.GetString("electrocuted-component-mob-shocked-by-source-popup-others",
                        ("mob", uid), ("source", (sourceUid.Value))), uid, filter, true);
                PlayElectrocutionSound(uid, sourceUid.Value);
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("electrocuted-component-mob-shocked-popup-others",
                    ("mob", uid)), uid, filter, true);
            }

            return true;
        }

        private void GetChainedElectrocutionTargets(EntityUid source, List<(EntityUid entity, int depth)> all)
        {
            var visited = new HashSet<EntityUid>();

            GetChainedElectrocutionTargetsRecurse(source, 1, visited, all);
        }

        private void GetChainedElectrocutionTargetsRecurse(
            EntityUid entity,
            int depth,
            HashSet<EntityUid> visited,
            List<(EntityUid entity, int depth)> all)
        {
            all.Add((entity, depth));
            visited.Add(entity);

            if (EntityManager.TryGetComponent(entity, out SharedPullableComponent? pullable)
                && pullable.Puller is {Valid: true} pullerId
                && !visited.Contains(pullerId))
            {
                GetChainedElectrocutionTargetsRecurse(pullerId, depth + 1, visited, all);
            }

            if (EntityManager.TryGetComponent(entity, out SharedPullerComponent? puller)
                && puller.Pulling is {Valid: true} pullingId
                && !visited.Contains(pullingId))
            {
                GetChainedElectrocutionTargetsRecurse(pullingId, depth + 1, visited, all);
            }
        }

        private void OnRandomInsulationMapInit(EntityUid uid, RandomInsulationComponent randomInsulation,
            MapInitEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out InsulatedComponent? insulated))
                return;

            if (randomInsulation.List.Length == 0)
                return;

            SetInsulatedSiemensCoefficient(uid, _random.Pick(randomInsulation.List), insulated);
        }

        private void PlayElectrocutionSound(EntityUid targetUid, EntityUid sourceUid, ElectrifiedComponent? electrified = null)
        {
            if (!Resolve(sourceUid, ref electrified) || !electrified.PlaySoundOnShock)
            {
                return;
            }

            SoundSystem.Play(electrified.ShockNoises.GetSound(), Filter.Pvs(targetUid), targetUid, AudioParams.Default.WithVolume(electrified.ShockVolume));
        }
    }
}

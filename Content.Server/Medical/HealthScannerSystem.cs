using Content.Server.Medical.Components;
using JetBrains.Annotations;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Content.Shared.MobState.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;
using Content.Server.DoAfter;

using static Content.Shared.HealthScanner.SharedHealthScannerComponent;

namespace Content.Server.Medical
{
    [UsedImplicitly]
    internal sealed class HealthScannerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HealthScannerComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<HealthScannerComponent, ActivateInWorldEvent>(HandleActivateInWorld);
            SubscribeLocalEvent<HealthScannerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<HealthScannerComponent, HealthComponentSyncRequestMessage>(OnSyncRequest);
            SubscribeLocalEvent<TargetScanSuccessfulEvent>(OnTargetScanSuccessful);
        }

        private void OnComponentInit(EntityUid uid, HealthScannerComponent healthScanner, ComponentInit args)
        {
            base.Initialize();
        }

        private void HandleActivateInWorld(EntityUid uid, HealthScannerComponent healthScanner, ActivateInWorldEvent args)
        {
            if (!TryComp<ActorComponent>(args.User, out ActorComponent? actor))
            {
                return;
            }

            healthScanner.UserInterface?.Open(actor.PlayerSession);
        }

        private void OnAfterInteract(EntityUid uid, HealthScannerComponent healthScanner, AfterInteractEvent args)
        {
            if (args.Target == null)
                return;

            if (!TryComp<MobStateComponent>(args.Target, out MobStateComponent? comp))
                return;

            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, healthScanner.ScanDelay, target: args.Target)
            {
                BroadcastFinishedEvent = new TargetScanSuccessfulEvent(args.User, args.Target, healthScanner),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
            });
        }

        private void OnTargetScanSuccessful(TargetScanSuccessfulEvent args)
        {
            SetHealthInfo(args.Target, args.Component);
        }

        private void OnSyncRequest(EntityUid uid, HealthScannerComponent healthScanner, HealthComponentSyncRequestMessage args)
        {
            healthScanner.UserInterface?.SendMessage(new HealthComponentDamageMessage(
                healthScanner.TargetName,
                healthScanner.TargetIsAlive,
                healthScanner.TotalDamage,
                healthScanner.DamageGroups));
        }

        private void SetHealthInfo(EntityUid? target, HealthScannerComponent scannerComponent)
        {
            var targetName = "Unknown";

            if (target == null || scannerComponent.UserInterface == null)
                return;

            if (!TryComp<DamageableComponent>(target, out var damageable))
                return;

            if (TryComp<MetaDataComponent>(target, out var meta))
            {
              targetName = meta.EntityName;
            }

            var DamagePerGroup = damageable.DamagePerGroup ?? new();
            var DamagePerType = damageable.Damage?.DamageDict ?? new();


            // Show the total damage and type breakdown for each damage group.
            List<MobDamageGroup> damageGroups = new();
            HashSet<string> shownTypes = new();

            foreach (var (damageGroupId, damageAmount) in DamagePerGroup)
            {
                MobDamageGroup damageType = new(damageGroupId, damageAmount.ToString(), new Dictionary<string, string>());
                // Show the damage for each type in that group.
                var group = _prototypeManager.Index<DamageGroupPrototype>(damageGroupId);
                foreach (var type in group.DamageTypes)
                {
                    if (DamagePerType.TryGetValue(type, out var typeAmount))
                    {
                        // If damage types are allowed to belong to more than one damage group, they may appear twice here. Mark them as duplicate.
                        if (!shownTypes.Contains(type))
                        {
                            shownTypes.Add(type);
                            if (damageType.GroupedMinorDamages != null)
                            {
                                damageType.GroupedMinorDamages.Add(type, typeAmount.ToString());
                            }
                        }
                    }
                }
                damageGroups.Add(damageType);
            }

            var totalDamage = 0;
            totalDamage = totalDamage = damageable.TotalDamage.Int();

            var isAlive = false;
            if (TryComp<MobStateComponent>(target, out var mobState))
            {
                isAlive = !mobState.IsDead();
            }

            scannerComponent.TargetName = targetName;
            scannerComponent.TargetIsAlive = isAlive;
            scannerComponent.TotalDamage = totalDamage.ToString();
            scannerComponent.DamageGroups = damageGroups;
            scannerComponent.UserInterface.SendMessage(new HealthComponentDamageMessage(targetName, isAlive, totalDamage.ToString(), damageGroups));
        }

        private sealed class TargetScanSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User { get; }
            public EntityUid? Target { get; }
            public HealthScannerComponent Component { get; }

            public TargetScanSuccessfulEvent(EntityUid user, EntityUid? target, HealthScannerComponent component)
            {
                User = user;
                Target = target;
                Component = component;
            }
        }
    }
}

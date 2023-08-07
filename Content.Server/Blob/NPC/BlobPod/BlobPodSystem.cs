using Content.Server.DoAfter;
using Content.Server.Explosion.EntitySystems;
using Content.Server.NPC.HTN;
using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Blob;
using Content.Shared.CombatMode;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Blob.NPC.BlobPod
{
    public sealed class BlobPodSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly MobStateSystem _mobs = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BlobPodComponent, GetVerbsEvent<InnateVerb>>(AddDrainVerb);
            SubscribeLocalEvent<BlobPodComponent, BlobPodZombifyDoAfterEvent>(OnZombify);
            SubscribeLocalEvent<BlobPodComponent, DestructionEventArgs>(OnDestruction);
        }

        private void OnDestruction(EntityUid uid, BlobPodComponent component, DestructionEventArgs args)
        {
            if (!TryComp<BlobCoreComponent>(component.Core, out var blobCoreComponent))
                return;
            if (blobCoreComponent.CurrentChem == BlobChemType.ExplosiveLattice)
            {
                _explosionSystem.QueueExplosion(uid, blobCoreComponent.BlobExplosive, 4, 1, 2, maxTileBreak: 0);
            }
        }

        private void AddDrainVerb(EntityUid uid, BlobPodComponent component, GetVerbsEvent<InnateVerb> args)
        {
            if (args.User == args.Target)
                return;
            if (!args.CanAccess)
                return;
            if (!HasComp<HumanoidAppearanceComponent>(args.Target))
                return;
            if (_mobs.IsAlive(args.Target))
                return;

            InnateVerb verb = new()
            {
                Act = () =>
                {
                    NpcStartZombify(uid, args.Target, component);
                },
                Text = Loc.GetString("blob-pod-verb-zombify"),
                // Icon = new SpriteSpecifier.Texture(new ("/Textures/")),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void OnZombify(EntityUid uid, BlobPodComponent component, BlobPodZombifyDoAfterEvent args)
        {
            component.IsZombifying = false;
            if (args.Handled || args.Args.Target == null)
            {
                component.ZombifyStingStream?.Stop();
                return;
            }

            if (args.Cancelled)
            {
                return;
            }

            _inventory.TryGetSlotEntity(args.Args.Target.Value, "head", out var headItem);
            if (HasComp<BlobMobComponent>(headItem))
                return;

            _inventory.TryUnequip(args.Args.Target.Value, "head", true, true);
            var equipped = _inventory.TryEquip(args.Args.Target.Value, uid, "head", true, true);

            if (!equipped)
                return;

            _popups.PopupEntity(Loc.GetString("blob-mob-zombify-second-end", ("pod", uid)), args.Args.Target.Value, args.Args.Target.Value, Shared.Popups.PopupType.LargeCaution);
            _popups.PopupEntity(Loc.GetString("blob-mob-zombify-third-end", ("pod", uid), ("target", args.Args.Target.Value)), args.Args.Target.Value, Filter.PvsExcept(args.Args.Target.Value), true, Shared.Popups.PopupType.LargeCaution);

            EntityManager.RemoveComponent<CombatModeComponent>(uid);

            EntityManager.RemoveComponent<HTNComponent>(uid);

            EntityManager.EnsureComponent<UnremoveableComponent>(uid);

            _audioSystem.PlayPvs(component.ZombifyFinishSoundPath, uid);

            var rejEv = new RejuvenateEvent();
            RaiseLocalEvent(args.Args.Target.Value, rejEv);

            component.ZombifiedEntityUid = args.Args.Target.Value;

            var zombieBlob = EnsureComp<ZombieBlobComponent>(args.Args.Target.Value);
            zombieBlob.BlobPodUid = uid;
        }


        public bool NpcStartZombify(EntityUid uid, EntityUid target, BlobPodComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;
            if (!HasComp<HumanoidAppearanceComponent>(target))
                return false;
            if (_mobs.IsAlive(target))
                return false;
            if (!_actionBlocker.CanInteract(uid, target))
                return false;

            StartZombify(uid, target, component);
            return true;
        }

        public void StartZombify(EntityUid uid, EntityUid target, BlobPodComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

           component.ZombifyTarget = target;
            _popups.PopupEntity(Loc.GetString("blob-mob-zombify-second-start", ("pod", uid)), target, target, Shared.Popups.PopupType.LargeCaution);
            _popups.PopupEntity(Loc.GetString("blob-mob-zombify-third-start", ("pod", uid), ("target", target)), target, Filter.PvsExcept(target), true, Shared.Popups.PopupType.LargeCaution);

            component.ZombifyStingStream = _audioSystem.PlayPvs(component.ZombifySoundPath, target);
            component.IsZombifying = true;

            var ev = new BlobPodZombifyDoAfterEvent();
            var args = new DoAfterArgs(uid, component.ZombifyDelay, ev, uid, target: target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = false,
                DistanceThreshold = 2f,
                NeedHand = false
            };

            _doAfter.TryStartDoAfter(args);
        }
    }
}

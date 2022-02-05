using System.Collections.Generic;
using Content.Server.Cloning.Components;
using Content.Server.Mind.Components;
using Content.Server.Power.Components;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Content.Server.Climbing;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.MobState.Components;
using Content.Shared.Species;
using Robust.Shared.Log;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Content.Server.EUI;
using Robust.Shared.Containers;


using static Content.Shared.Cloning.SharedCloningPodComponent;

namespace Content.Server.Cloning
{
    internal sealed class CloningPodSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPlayerManager _playerManager = null!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly EuiManager _euiManager = null!;

        public readonly Dictionary<Mind.Mind, EntityUid> ClonesWaitingForMind = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CloningPodComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(EntityUid uid, CloningPodComponent clonePod, ComponentInit args)
        {
            clonePod.BodyContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(clonePod.Owner, $"{Name}-bodyContainer");
        }

        private void UpdateAppearance(CloningPodComponent clonePod)
        {
            if (TryComp<AppearanceComponent>(clonePod.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(CloningPodVisuals.Status, clonePod.Status);
            }
        }

        public void TryCloning(Mind.Mind mind, HumanoidCharacterProfile hcp, CloningPodComponent clonePod)
        {
            if (clonePod.BodyContainer.ContainedEntity != null)
            {
                // obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("cloning-pod-component-msg-occupied"));
                return;
            }
            // if (message.ScanId == null)
            // {
                // obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("cloning-pod-component-msg-no-selection"));
            //     return;
            // }
            // var cloningSystem = EntitySystem.Get<CloningSystem>();
            // if (!cloningSystem.IdToDNA.TryGetValue(message.ScanId.Value, out var dna))
            // {
            //     obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("cloning-pod-component-msg-bad-selection"));
            //     return; // ScanId is not in database
            // }
            // var mind = dna.Mind;

            // MOVE TO CLONING SYSTEM
            if (ClonesWaitingForMind.TryGetValue(mind, out var clone))
            {
                if (EntityManager.EntityExists(clone) &&
                    TryComp<MobStateComponent>(clone, out var cloneState) &&
                    !cloneState.IsDead() &&
                    TryComp<MindComponent>(clone, out MindComponent? cloneMindComp) &&
                    (cloneMindComp.Mind == null || cloneMindComp.Mind == mind))
                {
                    // obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("cloning-pod-component-msg-already-cloning"));
                    return; // Mind already has clone
                }
                ClonesWaitingForMind.Remove(mind);
            }
            if (mind.OwnedEntity != null &&
                TryComp<MobStateComponent?>(mind.OwnedEntity.Value, out var state) &&
                !state.IsDead())
            {
                // obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("cloning-pod-component-msg-already-alive"));
                return; // Body controlled by mind is not dead
            }

            // Yes, we still need to track down the client because we need to open the Eui
            if (mind.UserId == null || !_playerManager.TryGetSessionById(mind.UserId.Value, out var client))
            {
                // obj.Session.AttachedEntity.Value.PopupMessageCursor(Loc.GetString("cloning-pod-component-msg-user-offline"));
                return; // If we can't track down the client, we can't offer transfer. That'd be quite bad.
            }



            if (!TryComp<TransformComponent>(clonePod.Owner, out var transform))
            {
                return;
            }
            var speciesProto = _prototype.Index<SpeciesPrototype>(hcp.Species).Prototype;
            var mob = EntityManager.SpawnEntity(speciesProto, transform.MapPosition);
            EntitySystem.Get<SharedHumanoidAppearanceSystem>().UpdateFromProfile(mob, hcp);
            if (!TryComp<MetaDataComponent>(mob, out var meta))
            {
                // FISH FIX THIS, NO DEPENDANT META DATA
                return;
            }
            meta.EntityName = hcp.Name;
            var cloneMindReturn = EntityManager.AddComponent<BeingClonedComponent>(mob);
            cloneMindReturn.Mind = mind;
            cloneMindReturn.Parent = clonePod.Owner;
            clonePod.BodyContainer.Insert(mob);
            clonePod.CapturedMind = mind;
            EntitySystem.Get<CloningSystem>().ClonesWaitingForMind.Add(mind, mob);
            UpdateStatus(CloningPodStatus.NoMind, clonePod);
            var acceptMessage = new AcceptCloningEui(mind);
            _euiManager.OpenEui(acceptMessage, client);
        }

        public bool IsPowered(CloningPodComponent clonepod)
        {
            if (!TryComp<ApcPowerReceiverComponent>(clonepod.Owner, out ApcPowerReceiverComponent? receiver))
            {
                return false;
            }
            return receiver.Powered;
        }

        public void Eject(CloningPodComponent clonePod)
        {
            if (clonePod.BodyContainer.ContainedEntity is not {Valid: true} entity || clonePod.CloningProgress < clonePod.CloningTime)
                return;

            EntityManager.RemoveComponent<BeingClonedComponent>(entity);
            clonePod.BodyContainer.Remove(entity);
            clonePod.CapturedMind = null;
            clonePod.CloningProgress = 0f;
            UpdateStatus(CloningPodStatus.Idle, clonePod);
            EntitySystem.Get<ClimbSystem>().ForciblySetClimbing(entity);
        }

        public void UpdateStatus(CloningPodStatus status, CloningPodComponent cloningPod)
        {
            cloningPod.Status = status;
            UpdateAppearance(cloningPod);
        }

        public override void Update(float frameTime)
        {
            foreach (var (cloning, power) in EntityManager.EntityQuery<CloningPodComponent, ApcPowerReceiverComponent>())
            {
                if (!IsPowered(cloning))
                    continue;

                if (cloning.BodyContainer.ContainedEntity != null)
                {
                    cloning.CloningProgress += frameTime;
                    cloning.CloningProgress = MathHelper.Clamp(cloning.CloningProgress, 0f, cloning.CloningTime);
                }

                if (cloning.CapturedMind?.Session?.AttachedEntity == cloning.BodyContainer.ContainedEntity)
                {
                    Eject(cloning);
                }
            }
        }

        struct ClonerDNAEntry {
            public Mind.Mind Mind;
            public HumanoidCharacterProfile Profile;

            public ClonerDNAEntry(Mind.Mind m, HumanoidCharacterProfile hcp)
            {
                Mind = m;
                Profile = hcp;
            }
        }
    }
}

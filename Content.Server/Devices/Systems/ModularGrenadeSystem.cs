using System.Linq;
using Content.Server.Construction.Components;
using Content.Server.Devices.Components;
using Content.Server.Explosion;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Server.Devices.Systems
{
    public class ModularGrenadeSystem : EntitySystem
    {

        [Dependency] private readonly TriggerSystem _triggerSystem = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<ModularGrenadeComponent, IoDeviceOutputEvent>(OnOutputReceived);
            SubscribeLocalEvent<ModularGrenadeComponent, UseInHandEvent>(OnUse);
        }

        //if the greande is 'used', we use the IoDevice in the trigger.
        //this may lead to some strange situations, such as a remote signaler being used as the trigger activating
        private void OnUse(EntityUid uid, ModularGrenadeComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            TryUseTrigger(uid, args, component);
        }

        //If the grenade is receiving IoOutput, that means it's ready to explode!
        private void OnOutputReceived(EntityUid uid, ModularGrenadeComponent grenadeComponent, IoDeviceOutputEvent args)
        {
            ActivateGrenade(uid, grenadeComponent);
        }

        public bool TryUseTrigger(EntityUid uid, UseInHandEvent args, ModularGrenadeComponent? grenadeComponent = null)
        {
            if (!Resolve(uid, ref grenadeComponent))
                return false;

            var owner = EntityManager.GetEntity(uid);

            if (!owner.TryGetComponent(out ContainerManagerComponent? containerManager))
            {
                Logger.Warning($"Modular Grenade entity {owner} did not have a container manager! Aborting trigger!");
                return false;
            }

            if (!containerManager.TryGetContainer(ModularGrenadeComponent.TriggerContainer, out var triggerContainer))
            {
                Logger.Warning($"Modular Grenade entity {owner} did not have the '{ModularGrenadeComponent.TriggerContainer}' container! Aborting trigger!");
                return false;
            }

            if (triggerContainer.ContainedEntities.Count <= 0)
            {
                args.User.PopupMessage("There is no trigger installed.");
                return false;
            }

            var trigger = triggerContainer.ContainedEntities[0];//did you know arrays start at 0 and not 1????
                                                                     //i've only been coding for 10 years!!!

            //we should only have 1 entity in the trigger slot, so just index the first one
            //we 'use' the trigger here. if that activates the grenade, we dunno.
            //some devices may not activate the grenade at all when used, but it is kinda funny.
            RaiseLocalEvent(trigger.Uid, args);

            args.Handled = true;

            return true;
        }

        public void ActivateGrenade(EntityUid uid, ModularGrenadeComponent? grenadeComponent = null,
            ContainerManagerComponent? containerManagerComponent = null)
        {
            if (!Resolve(uid, ref grenadeComponent, ref containerManagerComponent))
                return;

            var owner = EntityManager.GetEntity(uid);

            if (!containerManagerComponent.TryGetContainer(BombPayloadComponent.BombPayloadContainer,
                out var bombContainer))
            {
                owner.PopupMessageEveryone("*click*");
                return;
            }


            if (bombContainer.ContainedEntities.Count <= 0)
            {
                owner.PopupMessageEveryone("*click*");
                return;
            }

            _triggerSystem.Trigger(bombContainer.ContainedEntities[0]);

            //always remove the grenade after the payload is triggered.
            owner.QueueDelete();
        }
    }
}

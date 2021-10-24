using Content.Server.Actions.Actions;
using Content.Server.Lathe.Components;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Guardian;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using System;

namespace Content.Server.Guardian
{
    public class GuardianSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GuardianCreatorComponent, UseInHandEvent>(OnGuardianCreated);
            // SubscribeLocalEvent<SpecimenDietComponent, ComponentInit>(Initialize);
        }

        /// <summary>
        /// Adds the guardian host component to the user and spawns the guardian inside said component
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        private void OnGuardianCreated(EntityUid uid, GuardianCreatorComponent component, UseInHandEvent args)
        {
            //Only works if the guardian is already present. NOTE: this is up to preference, we can probably later allow
            //a person to host multiple guardians, however the idea is that in a normal setting you won't get the opportunity to
            if (!args.User.TryGetComponent<GuardianHostComponent>(out var guardianComponent))
            {
                if (!args.User.TryGetComponent<GuardianSharedComponent>(out var sharedguardian))
                {
                    if (args.User.TryGetComponent<SharedActionsComponent>(out SharedActionsComponent? action))
                    {
                        var host = args.User.AddComponent<GuardianHostComponent>();
                        var guardian = EntityManager.SpawnEntity(component.GuardianType, host.Owner.Transform.Coordinates);
                        host.GuardianContainer.Insert(guardian);
                        var shared = args.User.AddComponent<GuardianSharedComponent>();
                        shared.Guardian = guardian.Uid;
                        shared.Host = args.User.Uid;
                        shared.AllowedDistance = component.GuardianTetherDistance;
                        var guardianshared = guardian.AddComponent<GuardianSharedComponent>();
                        guardianshared.Guardian = guardian.Uid;
                        guardianshared.Host = args.User.Uid;
                        guardianshared.AllowedDistance = component.GuardianTetherDistance;
                        args.User.PopupMessage(Loc.GetString("guardian-created"));
                        action.Grant(ActionType.ManifestGuardian);
                    }
                    else
                    {
                        args.User.PopupMessage(Loc.GetString("guardian-already-present-invalid-creation"));
                    }
                }
                else
                {
                    args.User.PopupMessage(Loc.GetString("guardian-already-present-invalid-creation"));
                }
            }
            else
            {
                args.User.PopupMessage(Loc.GetString("guardian-already-present-invalid-creation"));
            }
        }

        public void OnGuardianManifestAction(EntityUid guardian, EntityUid host, bool guardianloose)
        {
            //the loose parameter toggling is inside the action
            if (guardianloose == false)
            {
                //Ejects the guardian if it isn't inside
                EntityManager.GetEntity(host).GetComponent<GuardianHostComponent>().EjectBody();  
            }
            else if (guardianloose == true)
            {
                //Recalls guardian if it's outside
                //Message first otherwise it's a dead giveaway
                EntityManager.GetEntity(guardian).PopupMessageEveryone(Loc.GetString("guardian-entity-recall"));
                EntityManager.GetEntity(host).GetComponent<GuardianHostComponent>().GuardianContainer.Insert(EntityManager.GetEntity(guardian));         
            }
        }
    }
}

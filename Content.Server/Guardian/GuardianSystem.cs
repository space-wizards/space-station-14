using Content.Server.Actions.Actions;
using Content.Server.Lathe.Components;
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
            if (!args.User.TryGetComponent<GuardianHostComponent>(out var guardianComponent))
            {
                var host = args.User.AddComponent<GuardianHostComponent>();
                var guardian = EntityManager.SpawnEntity(component.GuardianType, host.Owner.Transform.Coordinates);
                host.GuardianContainer.Insert(guardian);
                args.User.PopupMessage(Loc.GetString("guardian-created"));
            }
            else
            {
                args.User.PopupMessage(Loc.GetString("guardian-already-present-invalid-creation"));
            }
        }

        public void OnGuardianManifestAction(IEntity guardian)
        {
            
        }
    }
}

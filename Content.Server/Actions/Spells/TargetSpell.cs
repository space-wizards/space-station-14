using Content.Server.Utility;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.Actions
{
    public class TargetSpell : ITargetEntityAction
    {

        [ViewVariables] [DataField("castmessage")] public string CastMessage { get; set; } = "Instant action used.";
        [ViewVariables] [DataField("castrange")] public float CastRange { get; set; } = 5f;
        [ViewVariables] [DataField("cooldown")] public float CoolDown { get; set; } = 1f;
        [ViewVariables] [DataField("NeedComponent")] public string TargetType { get; set; } = "Mind";
        [ViewVariables] [DataField("AddedComponent")] public string InduceComponent { get; set; } = "RadiatonPulse";
        [ViewVariables] [DataField("castsound")] public string castSound { get; set; } = "/Audio/Effects/Fluids/slosh.ogg";

        public Type? RegisteredTargetType;

        public Type? RegisteredInduceType;

        public IComponent? CheckedComponent;

        public void DoTargetEntityAction(TargetEntityActionEventArgs args)
        {
            var compFactory = IoCManager.Resolve<IComponentFactory>();
            var registration = compFactory.GetRegistration(TargetType);
            RegisteredTargetType = registration.Type;
            //For the inducer
            var registrationInducer = compFactory.GetRegistration(InduceComponent);
            RegisteredInduceType = registrationInducer.Type;
            var caster = args.Performer;
            var target = args.Target;
            //For the range of the spell
            var casterCoords = caster.Transform.WorldPosition;
            var targetCoords = target.Transform.WorldPosition;
            var effectiveRange = (casterCoords - targetCoords).Length;
            if (!caster.TryGetComponent<SharedActionsComponent>(out var actions)) return;
            if (CastRange < effectiveRange && CastRange != 0)
            {
                caster.PopupMessage("This spell cannot reach this far!");
                return;
            }

            //Now the fun part, actually applying the spell component to the caster
            if (!target.TryGetComponent(RegisteredTargetType, out var component))
            {
                caster.PopupMessage("Your hands fizzle, your target is invalid!");
                return;
            }
            if (target.HasComponent(RegisteredInduceType))
            {
                caster.PopupMessage("This poor soul is already cursed!");
                return;
            }
            caster.PopupMessageEveryone(CastMessage);
            actions.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(CoolDown)); //Set the spell on cooldown
            var componentInduced = compFactory.GetComponent(RegisteredInduceType);
            Component compInducedFinal = (Component)componentInduced;
            compInducedFinal.Owner = target;
           // EntitySystem.Get<AudioSystem>().PlayFromEntity(castSound, caster);
            target.EntityManager.ComponentManager.AddComponent(target, compInducedFinal);
        }

    }
}

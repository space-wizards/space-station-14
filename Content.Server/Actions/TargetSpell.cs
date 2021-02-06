using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.Utility;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Physics;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using System;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    public class TargetSpell : ITargetEntityAction
    {
        //[Dependency] private readonly IEntityManager _entityManager = default!;

        public string CastMessage { get; private set; }
        public float CastRange { get; private set; }
        public float CoolDown { get; private set; }
        public bool IgnoreCaster { get; private set; }
        public string TargetType { get; private set; }
        public string InduceComponent { get; private set; }

        public Type RegisteredTargetType;

        public Type RegisteredInduceType;

        public IComponent CheckedComponent; 

        public TargetSpell()
        {
           
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.CastMessage, "castmessage", "Instant action used."); //What player says upon casting the spell
            serializer.DataField(this, x => x.CastRange, "castrange", 5f); //The rage at which the spell is cast (leave at below 0 for unlimited range, 0 for touch, etc)
            serializer.DataField(this, x => x.CoolDown, "cooldown", 0f); //Cooldown of the spell
            serializer.DataField(this, x => x.TargetType, "NeedComponent", "SharedActionsComponent"); //Needed component the target must posess
            serializer.DataField(this, x => x.InduceComponent, "AddedComponent", "SharedActionsComponent"); //The component the spell adds onto the target
        }

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
            if (!caster.TryGetComponent<SharedActionsComponent>(out var actions)) return;


            //caster.PopupMessageEveryone(CastMessage); //Speak the cast message out loud
            //Now the fun part, actually applying the spell component to the caster
            if (!target.TryGetComponent(RegisteredTargetType, out var component)) return;
            actions.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(CoolDown)); //Set the spell on cooldown
            caster.PopupMessageEveryone(CastMessage);
            var IComponent InduceComp = RegisteredInduceType.
            target.EnsureComponent<RegisteredInduceType>;
        }

    }
}

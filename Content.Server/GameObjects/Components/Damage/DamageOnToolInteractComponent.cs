using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Damage
{
    [RegisterComponent]
    class DamageOnToolInteractComponent : Component, IInteractUsing
    {
        public override string Name => "DamageOnToolInteract";

        /* Set in YAML */
        protected int Damage;
        private List<ToolQuality> _tools = new List<ToolQuality>();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref Damage, "damage", 0);

            serializer.DataField(ref _tools, "tools", new List<ToolQuality>());
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<DamageableComponent>();
        }

        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            

            if (eventArgs.Using.TryGetComponent<ToolComponent>(out var tool))
            {
                if (tool.HasQuality(ToolQuality.Welding))
                {
                    if (eventArgs.Using.TryGetComponent<WelderComponent>(out WelderComponent welder))
                    {
                        if (welder.WelderLit)
                            return CallDamage(eventArgs);
                        else
                            return false;
                    }

                    //Should I add some error checking here for TryGet?
                }

                foreach (var toolQuality in _tools)
                {
                    if (tool.HasQuality(toolQuality))
                        return CallDamage(eventArgs); //Break when return called automatically.
                }
            }
            return false;
        }

        protected bool CallDamage(InteractUsingEventArgs eventArgs, ToolComponent tool)
        {
            if (eventArgs.Target.TryGetComponent<DamageableComponent>(out var damageable))
            {
                if(tool.HasQuality(ToolQuality.Welding)) damageable.TakeDamage(Shared.GameObjects.DamageType.Heat, Damage, eventArgs.Using, eventArgs.User);
                else
                damageable.TakeDamage(Shared.GameObjects.DamageType.Brute, Damage, eventArgs.Using, eventArgs.User);
                return true;
            }
                return false;
        }
    }
}

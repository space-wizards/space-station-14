using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Tools.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Tool;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public class DamageOnToolInteractComponent : Component, IInteractUsing
    {

        public override string Name => "DamageOnToolInteract";

        [DataField("damage")]
        protected int Damage;

        [DataField("tools")]
        private List<ToolQuality> _tools = new();

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
=======
=======
>>>>>>> refactor-damageablecomponent
        // TODO PROTOTYPE Replace these datafield variable with prototype references, once they are supported.
        // Also remove Initialize override, if no longer needed.
        [DataField("weldingDamageType")]
        private readonly string _weldingDamageTypeID = "Heat";
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageTypePrototype WeldingDamageType = default!;
        [DataField("defaultDamageType")]
        private readonly string _defaultDamageTypeID = "Blunt";
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageTypePrototype DefaultDamageType = default!;
        protected override void Initialize()
        {
            base.Initialize();
            WeldingDamageType = IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>(_weldingDamageTypeID);
            DefaultDamageType = IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>(_defaultDamageTypeID);
        }

<<<<<<< HEAD
>>>>>>> Refactor damageablecomponent update (#4406)
=======
>>>>>>> refactor-damageablecomponent
        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (eventArgs.Using.TryGetComponent<ToolComponent>(out var tool))
            {
                foreach (var toolQuality in _tools)
                {
                    if (tool.HasQuality(ToolQuality.Welding) && toolQuality == ToolQuality.Welding)
                    {
                        if (eventArgs.Using.TryGetComponent(out WelderComponent? welder))
                        {
                            if (welder.WelderLit) return CallDamage(eventArgs, tool);
                        }
                        break; //If the tool quality is welding and its not lit or its not actually a welder that can be lit then its pointless to continue.
                    }

                    if (tool.HasQuality(toolQuality)) return CallDamage(eventArgs, tool);
                }
            }
            return false;
        }

        protected bool CallDamage(InteractUsingEventArgs eventArgs, ToolComponent tool)
        {
            if (!eventArgs.Target.TryGetComponent<IDamageableComponent>(out var damageable))
                return false;

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
                return true;
            }
=======
=======
>>>>>>> refactor-damageablecomponent
            damageable.TryChangeDamage(tool.HasQuality(ToolQuality.Welding)
                    ? WeldingDamageType
                    : DefaultDamageType,
                Damage);
<<<<<<< HEAD
>>>>>>> Refactor damageablecomponent update (#4406)
=======
>>>>>>> refactor-damageablecomponent

            return true;
        }
    }
}

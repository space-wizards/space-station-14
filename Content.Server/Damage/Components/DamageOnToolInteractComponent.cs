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

        //TODO PROTOTYPE Replace this code with prototype references, once they are supported.
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [DataField("weldingDamageType",required: true)]
        private readonly string _weldingDamageTypeID = default!;
        private DamageTypePrototype _weldingDamageType => _prototypeManager.Index<DamageTypePrototype>(_weldingDamageTypeID);
        [DataField("defaultDamageType",required: true)]
        private readonly string _defaultDamageTypeID = default!;
        private DamageTypePrototype _defaultDamageType => _prototypeManager.Index<DamageTypePrototype>(_defaultDamageTypeID);

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

            damageable.ChangeDamage(tool.HasQuality(ToolQuality.Welding)
                    ? _weldingDamageType
                    : _defaultDamageType,
                Damage, false, eventArgs.User);

            return true;
        }
    }
}

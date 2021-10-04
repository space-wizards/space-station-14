using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Tools.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Tool;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public class DamageOnToolInteractComponent : Component, IInteractUsing
    {
        public override string Name => "DamageOnToolInteract";

        [DataField("tools")]
        private List<ToolQuality> _tools = new();

        [DataField("weldingDamage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier? WeldingDamage;

        [DataField("defaultDamage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier? DefaultDamage;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (eventArgs.Using.TryGetComponent<ToolComponent>(out var tool))
            {
                foreach (var toolQuality in _tools)
                {
                    if (WeldingDamage != null && tool.HasQuality(ToolQuality.Welding) && toolQuality == ToolQuality.Welding)
                    {
                        if (eventArgs.Using.TryGetComponent(out WelderComponent? welder) && welder.WelderLit)
                        {
                            EntitySystem.Get<DamageableSystem>().TryChangeDamage(eventArgs.Target.Uid, WeldingDamage);
                            return true;
                        }
                        break; //If the tool quality is welding and its not lit or its not actually a welder that can be lit then its pointless to continue.
                    }

                    if (DefaultDamage != null && tool.HasQuality(toolQuality))
                    {
                        EntitySystem.Get<DamageableSystem>().TryChangeDamage(eventArgs.Target.Uid, DefaultDamage);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Interactable;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Damage
{
    [RegisterComponent]
    public class DamageOnToolInteractComponent : Component, IInteractUsing
    {
        public override string Name => "DamageOnToolInteract";

        [DataField("damage")]
        protected int Damage;

        [DataField("tools")]
        private List<ToolQuality> _tools = new();

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
            if (eventArgs.Target.TryGetComponent<IDamageableComponent>(out var damageable))
            {
                damageable.ChangeDamage(tool.HasQuality(ToolQuality.Welding)
                        ? DamageType.Heat
                        : DamageType.Blunt,
                    Damage, false, eventArgs.User);

                return true;
            }

            return false;
        }
    }
}

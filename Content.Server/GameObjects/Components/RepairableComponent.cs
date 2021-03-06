#nullable enable
using System.Threading.Tasks;
using Content.Shared.Interfaces;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Server.GameObjects.Components.Interactable;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class RepairableComponent : Component, IInteractUsing
    {
        public override string Name => "Repairable";

        [ViewVariables(VVAccess.ReadWrite)] [DataField("fuelCost")]
        private int _fuelCost = 5;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("doAfterDelay")]
        private int _doAfterDelay = 1;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            // Only repair if you are using a lit welder
            if (!eventArgs.Using.TryGetComponent(out WelderComponent? welder) || !welder.WelderLit)
                return false;

            if (Owner.TryGetComponent(out IDamageableComponent? damageable))
            {
                // Repair the target if it is damaged, oherwise do nothing
                if (damageable.TotalDamage > 0)
                {
                    if (!await welder.UseTool(eventArgs.User, Owner, _doAfterDelay, ToolQuality.Welding, _fuelCost))
                        return false;
                    damageable.Heal();

                    Owner.PopupMessage(eventArgs.User,
                        Loc.GetString("comp-repairable-repair",
                            ("target", Owner),
                            ("welder", eventArgs.Using)));
                }
            }
            return true;
        }
    }
}

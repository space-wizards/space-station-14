#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.PDA;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Mobs;
using Content.Server.Mobs.Roles;
using Content.Server.Mobs.Roles.Suspicion;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Suspicion;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.TraitorDeathMatch
{
    [RegisterComponent]
    public class TraitorDeathMatchRedemptionComponent : Component, IInteractUsing
    {
        /// <inheritdoc />
        public override string Name => "TraitorDeathMatchRedemption";

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var itemEntity = eventArgs.User.GetComponent<HandsComponent>().GetActiveHand?.Owner;

            if (itemEntity == null)
            {
                eventArgs.User.PopupMessage(Loc.GetString("You have no active hand!"));
                return false;
            }

            if (!itemEntity.TryGetComponent<PDAComponent>(out var pda))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("It must be a PDA!"));
                return false;
            }

            pda.Owner.Delete();
            Owner.PopupMessage(eventArgs.User, Loc.GetString("TODO: ACTION"));
            return true;
        }
    }
}

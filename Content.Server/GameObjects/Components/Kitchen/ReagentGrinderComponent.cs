#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Kitchen;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Kitchen
{

    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ReagentGrinderComponent : SharedReagentGrinderComponent, IActivate, IInteractUsing
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;


        private List<string> _grindableIds = new List<string>();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);    
            serializer.DataField(ref _grindableIds, "grind_list", new List<string>());
        }

        public override void Initialize()
        {
            base.Initialize();
            //ensure container for beaker?
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            //throw new NotImplementedException();
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {

            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor) || !Powered)
            {
                return false;
            }

            var heldEnt = eventArgs.Using;

            if (!_grindableIds.Contains(heldEnt.Prototype.ID)) return false;

            return true;
        }
    }
}

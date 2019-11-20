using System;
using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Content.Server.GameObjects.Components.Explosive;
using Content.Server.GameObjects.Components.Interactable.Tools;
using Content.Server.GameObjects.Components.Chemistry;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Content.Shared.Interfaces;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    class FuelTankComponent : Component, IAttackBy
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649
        public override string Name => "FuelTank";
        private string WelderFuelReagentID = "chem.WelderFuel";
        private SolutionComponent _solutionComponent;
        private ExplosiveComponent _explosive;
        private float _solutionAmt;  

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.TryGetComponent<SolutionComponent>(out _solutionComponent);
            Owner.TryGetComponent<ExplosiveComponent>(out _explosive);
        }
        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            var item = eventArgs.AttackWith;
            if(item.TryGetComponent<WelderComponent>(out var welder))
            {
                if (welder.Activated && _explosive != null)
                {
                    _explosive.Explosion();
                }

                if(!welder.Activated && item.TryGetComponent<SolutionComponent>(out var solution))
                {
                    var needed = solution.MaxVolume - solution.CurrentVolume;
                    if (needed > 0)
                    {
                        _solutionComponent.TryRemoveReagent(WelderFuelReagentID, needed);
                        solution.TryAddReagent(WelderFuelReagentID, needed, out var accepted); //Transfering is not a thing yet, doing it manually insteed.
                        _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/effects/refill.ogg", Owner);
                        Owner.PopupMessage(eventArgs.User, "Refueled with " + needed.ToString() + " fuel.");
                    }

                }
            }
            return false;

        }
    }
}

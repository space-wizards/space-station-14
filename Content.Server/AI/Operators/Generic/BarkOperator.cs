using Content.Server.AI.Utility.AiLogic;
using Content.Server.GameObjects.Components.Movement;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Operators.Generic
{
    public class BarkOperator : IOperator
    {
        private IEntity _owner;
        private BarkType _barkType;

        public BarkOperator(IEntity owner, BarkType barkType)
        {
            _owner = owner;
            _barkType = barkType;
        }
        
        public Outcome Execute(float frameTime)
        {
            if (!_owner.TryGetComponent(out AiControllerComponent aiController))
            {
                return Outcome.Failed;
            }

            if (aiController.Processor is UtilityAi utilityAi)
            {
                utilityAi.Bark(_barkType);
            }
            
            return Outcome.Success;
        }
    }

    /// <summary>
    /// Each AI might respond to a bark differently so we'll pass in
    /// the action type (e.g. Reloading) and then they can respond in their own way.
    /// </summary>
    public enum BarkType
    {
        
    }
}
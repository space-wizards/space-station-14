using System;
﻿using Content.Server.Objectives.Interfaces;
using Content.Server.Traitor;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.GameObjects;
using Content.Server.GameTicking.Rules;

namespace Content.Server.Objectives.Requirements
{
    [DataDefinition]
    public class MultipleTraitorsRequirement : IObjectiveRequirement
    {
        [DataField("traitors")]
        private readonly int _requiredTraitors = 2;
        
        public bool CanBeAssigned(Mind.Mind mind)
        {
                Console.WriteLine("Value of _requiredTraitors is: " + _requiredTraitors);
                Console.WriteLine("Value of EntitySystem.Get<TraitorRuleSystem>().TotalTraitors is :" + EntitySystem.Get<TraitorRuleSystem>().TotalTraitors);
                if (EntitySystem.Get<TraitorRuleSystem>().TotalTraitors >= _requiredTraitors)
                {
                        return true;
                }
                return false;
        }
        
    }
}

using System;
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    public class StealCondition : IObjectiveCondition
    {
        public string PrototypeID { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.PrototypeID, "prototype", "");
        }

        public string GetTitle() => $"Steal prototype {PrototypeID}";

        public string GetDescription() => $"We need you to steal prototype {PrototypeID}. Dont get caught.";

        public SpriteSpecifier GetIcon()
        {
            //TODO
            throw new NotImplementedException();
        }

        public float GetProgress(Mind mind)
        {
            //TODO
            throw new NotImplementedException();
        }

        public float GetDifficulty() => 1f;
    }
}

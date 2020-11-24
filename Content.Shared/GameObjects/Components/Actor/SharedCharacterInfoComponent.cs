using System;
using System.Collections.Generic;
using Content.Shared.Objectives;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Actor
{
    public class SharedCharacterInfoComponent : Component
    {
        public override string Name => "CharacterInfo";
        public override uint? NetID => ContentNetIDs.CHARACTERINFO;

        [Serializable, NetSerializable]
        protected class RequestCharacterInfoMessage : ComponentMessage
        {
            public RequestCharacterInfoMessage()
            {
                Directed = true;
            }
        }

        [Serializable, NetSerializable]
        protected class CharacterInfoMessage : ComponentMessage
        {
            public readonly Dictionary<string, List<ConditionInfo>> Objectives;
            public readonly string JobTitle;

            public CharacterInfoMessage(string jobTitle, Dictionary<string, List<ConditionInfo>> objectives)
            {
                Directed = true;
                JobTitle = jobTitle;
                Objectives = objectives;
            }
        }
    }
}

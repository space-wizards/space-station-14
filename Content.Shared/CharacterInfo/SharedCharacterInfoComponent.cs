using System;
using System.Collections.Generic;
using Content.Shared.Objectives;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterInfo
{
    [NetworkedComponent()]
    public class SharedCharacterInfoComponent : Component
    {
        public override string Name => "CharacterInfo";

        [Serializable, NetSerializable]
#pragma warning disable 618
        protected class RequestCharacterInfoMessage : ComponentMessage
#pragma warning restore 618
        {
            public RequestCharacterInfoMessage()
            {
                Directed = true;
            }
        }

        [Serializable, NetSerializable]
#pragma warning disable 618
        protected class CharacterInfoMessage : ComponentMessage
#pragma warning restore 618
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

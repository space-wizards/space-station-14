using Content.Server.GameObjects.Components.Interactable;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Content.Server.GameObjects.EntitySystems
{
    class RadioSystem : EntitySystem
    {
        private List<string> _messages;

        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(RadioComponent));
            _messages = new List<string>();
        }

        public void SpreadMessage(IEntity source, string message)
        {
            if (_messages.Contains(message))
            {
                return;
            }

            _messages.Add(message);

            foreach (var radioEntity in RelevantEntities)
            {
                var radio = radioEntity.GetComponent<RadioComponent>();
                if (radioEntity == source || !radio.RadioOn)
                {
                    continue;
                }

                radio.Speaker(message);
            }

            _messages.Remove(message);
        }
    }
}

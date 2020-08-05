using Content.Server.GameObjects.Components.Interactable;
using Content.Server.Interfaces;
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

            _messages = new List<string>();
        }

        public void SpreadMessage(IRadio source, string message)
        {
            if (_messages.Contains(message)) { return; }

            _messages.Add(message);

            foreach (var radio in ComponentManager.EntityQuery<IRadio>())
            {
                if (radio == source) { continue; }
                radio.Receiver(message);
            }
            _messages.Remove(message);
        }
    }
}

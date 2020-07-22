using Content.Server.GameObjects.Components.Interactable;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Shared.GameObjects.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Content.Server.GameObjects.EntitySystems
{
    class RadioSystem : EntitySystem
    {
        private List<RadioComponent> _activeRadios;
        private List<string> _messages;

        public override void Initialize()
        {
            base.Initialize();

            _activeRadios = new List<RadioComponent>();
            _messages = new List<string>();
        }

        public void Subscribe(RadioComponent radio)
        {
            if (_activeRadios.Contains(radio))
            {
                return;
            }

            _activeRadios.Add(radio);
        }

        public void Unsubscribe(RadioComponent radio)
        {
            if (!_activeRadios.Contains(radio))
            {
                return;
            }

            _activeRadios.Remove(radio);
        }

        public void SpreadMessage(RadioComponent source, string message)
        {
            if (_messages.Contains(message))
            {
                return;
            }

            _messages.Add(message);

            foreach (var radio in _activeRadios)
            {
                if (radio == source)
                {
                    continue;
                }

                radio.Speaker(message);
            }

            _messages.Remove(message);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Interactable;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class HandHeldLightSystem : EntitySystem
    {
        // TODO: Ideally you'd be able to subscribe to power stuff to get events at certain percentages.. or something?
        // But for now this will be better anyway.
        private HashSet<HandheldLightComponent> _activeLights = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActivateHandheldLightMessage>(HandleActivate);
            SubscribeLocalEvent<DeactivateHandheldLightMessage>(HandleDeactivate);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _activeLights.Clear();
            UnsubscribeLocalEvent<ActivateHandheldLightMessage>();
            UnsubscribeLocalEvent<DeactivateHandheldLightMessage>();
        }

        private void HandleActivate(ActivateHandheldLightMessage message)
        {
            _activeLights.Add(message.Component);
        }

        private void HandleDeactivate(DeactivateHandheldLightMessage message)
        {
            _activeLights.Remove(message.Component);
        }

        public override void Update(float frameTime)
        {
            foreach (var handheld in _activeLights.ToArray())
            {
                if (handheld.Deleted || handheld.Paused) continue;
                handheld.OnUpdate(frameTime);
            }
        }
    }
}

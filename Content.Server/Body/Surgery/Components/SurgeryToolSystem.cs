using System.Collections.Generic;
using Content.Server.Body.Surgery.Messages;
using Content.Shared.ActionBlocker;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Helpers;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Surgery.Components
{
    [UsedImplicitly]
    public class SurgeryToolSystem : EntitySystem, IResettingEntitySystem
    {
        private readonly HashSet<SurgeryToolComponent> _openSurgeryUIs = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SurgeryWindowOpenMessage>(OnSurgeryWindowOpen);
            SubscribeLocalEvent<SurgeryWindowCloseMessage>(OnSurgeryWindowClose);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<SurgeryWindowOpenMessage>();
            UnsubscribeLocalEvent<SurgeryWindowCloseMessage>();
        }

        public void Reset()
        {
            _openSurgeryUIs.Clear();
        }

        private void OnSurgeryWindowOpen(SurgeryWindowOpenMessage ev)
        {
            _openSurgeryUIs.Add(ev.Tool);
        }

        private void OnSurgeryWindowClose(SurgeryWindowCloseMessage ev)
        {
            _openSurgeryUIs.Remove(ev.Tool);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var tool in _openSurgeryUIs)
            {
                if (tool.PerformerCache == null)
                {
                    continue;
                }

                if (tool.BodyCache == null)
                {
                    continue;
                }

                if (!ActionBlockerSystem.CanInteract(tool.PerformerCache) ||
                    !tool.PerformerCache.InRangeUnobstructed(tool.BodyCache))
                {
                    tool.CloseAllSurgeryUIs();
                }
            }
        }
    }
}

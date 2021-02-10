using System.Collections.Generic;
using Content.Server.GameObjects.Components.Body.Surgery;
using Content.Server.GameObjects.Components.Body.Surgery.Messages;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameTicking;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.Body.Surgery
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

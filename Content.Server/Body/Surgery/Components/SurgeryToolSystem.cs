using System.Collections.Generic;
using Content.Server.Body.Surgery.Messages;
using Content.Shared.ActionBlocker;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction.Helpers;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Body.Surgery.Components
{
    [UsedImplicitly]
    public class SurgeryToolSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        private readonly HashSet<SurgeryToolComponent> _openSurgeryUIs = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<SurgeryWindowOpenMessage>(OnSurgeryWindowOpen);
            SubscribeLocalEvent<SurgeryWindowCloseMessage>(OnSurgeryWindowClose);
        }

        public void Reset(RoundRestartCleanupEvent ev)
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

                if (!_actionBlockerSystem.CanInteract(tool.PerformerCache.Uid) ||
                    !tool.PerformerCache.InRangeUnobstructed(tool.BodyCache))
                {
                    tool.CloseAllSurgeryUIs();
                }
            }
        }
    }
}

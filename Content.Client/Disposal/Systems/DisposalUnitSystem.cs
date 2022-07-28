using System.Collections.Generic;
using Content.Client.Disposal.Components;
using Content.Client.Disposal.UI;
using Content.Shared.Disposal;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Disposal.Systems
{
    public sealed class DisposalUnitSystem : SharedDisposalUnitSystem
    {
        public List<DisposalUnitComponent> PressuringDisposals = new();

        public void UpdateActive(DisposalUnitComponent component, bool active)
        {
            if (active)
            {
                if (!PressuringDisposals.Contains(component))
                    PressuringDisposals.Add(component);
            }
            else
            {
                PressuringDisposals.Remove(component);
            }
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            for (var i = PressuringDisposals.Count - 1; i >= 0; i--)
            {
                var comp = PressuringDisposals[i];
                if (!UpdateInterface(comp)) continue;
                PressuringDisposals.RemoveAt(i);
            }
        }

        private bool UpdateInterface(DisposalUnitComponent component)
        {
            if (component.Deleted) return true;

            if (!EntityManager.TryGetComponent(component.Owner, out ClientUserInterfaceComponent? userInterface)) return true;

            var state = component.UiState;
            if (state == null) return true;

            foreach (var inter in userInterface.Interfaces)
            {
                if (inter is DisposalUnitBoundUserInterface boundInterface)
                {
                    return boundInterface.UpdateWindowState(state) != false;
                }
            }

            return true;
        }
    }
}

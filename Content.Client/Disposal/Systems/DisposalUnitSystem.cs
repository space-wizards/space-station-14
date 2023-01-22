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
        private List<EntityUid> PressuringDisposals = new();

        public void UpdateActive(EntityUid disposalEntity, bool active)
        {
            if (active)
            {
                if (!PressuringDisposals.Contains(disposalEntity))
                    PressuringDisposals.Add(disposalEntity);
            }
            else
            {
                PressuringDisposals.Remove(disposalEntity);
            }
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            for (var i = PressuringDisposals.Count - 1; i >= 0; i--)
            {
                var disposal = PressuringDisposals[i];
                if (!UpdateInterface(disposal))
                    continue;

                PressuringDisposals.RemoveAt(i);
            }
        }

        private bool UpdateInterface(EntityUid disposalUnit)
        {
            if (!TryComp(disposalUnit, out DisposalUnitComponent? component) || component.Deleted)
                return true;
            if (component.Deleted)
                return true;
            if (!TryComp(disposalUnit, out ClientUserInterfaceComponent? userInterface))
                return true;

            var state = component.UiState;
            if (state == null)
                return true;

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

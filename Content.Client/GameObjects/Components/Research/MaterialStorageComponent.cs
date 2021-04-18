using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Research;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Research
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMaterialStorageComponent))]
    public class MaterialStorageComponent : SharedMaterialStorageComponent
    {
        protected override Dictionary<string, int> Storage { get; set; } = new();

        public event Action? OnMaterialStorageChanged;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not MaterialStorageState state) return;
            Storage = state.Storage;
            OnMaterialStorageChanged?.Invoke();
        }
    }
}

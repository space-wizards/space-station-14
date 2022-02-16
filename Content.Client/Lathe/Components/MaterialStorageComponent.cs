using System;
using System.Collections.Generic;
using Content.Shared.Lathe;
using Robust.Shared.GameObjects;

namespace Content.Client.Lathe.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMaterialStorageComponent))]
    public sealed class MaterialStorageComponent : SharedMaterialStorageComponent
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

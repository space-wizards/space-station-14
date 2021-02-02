#nullable enable

using System;
using System.Collections.Generic;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [RegisterComponent]
    public class StackedStorageComponent : Component, IMapInit
    {
        public override string Name => "SingleItemStorage";

        private string? _prototypeName;
        private int _amount;

        [ComponentDependency] private readonly IStorageComponent? _storageComponent = default;
        [ComponentDependency] private readonly AppearanceComponent? _appearanceComponent = default;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _prototypeName, "name", null);
            serializer.DataField(ref _amount, "amount", 1);
        }

        void IMapInit.MapInit()
        {
            if (!Owner.TryGetComponent(out IStorageComponent? storage))
            {
                Logger.Error($"StorageFillComponent couldn't find any StorageComponent ({Owner})");
                return;
            }

            if (string.IsNullOrEmpty(_prototypeName)) return;

            for (var i = 0; i < _amount; i++)
            {
                storage.Insert(Owner.EntityManager.SpawnEntity(_prototypeName, Owner.Transform.Coordinates));
            }
        }
    }
}
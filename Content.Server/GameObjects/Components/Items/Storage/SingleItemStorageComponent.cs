#nullable enable

using System.Collections.Generic;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Storage;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [RegisterComponent]
    public class SingleItemStorageComponent : Component, IMapInit
    {
        private string? _prototypeName;
        private int _amount;

        [ComponentDependency] private readonly ServerStorageComponent? _storage = default;
        [ComponentDependency] private readonly AppearanceComponent? _appearanceComponent = default;

        public override string Name => "SingleItemStorage";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _prototypeName, "name", null);
            serializer.DataField(ref _amount, "amount", 1);
        }

        void IMapInit.MapInit()
        {
            if (_storage == null)
            {
                Logger.Error($"StorageFillComponent couldn't find any StorageComponent ({Owner})");
                return;
            }

            if (string.IsNullOrEmpty(_prototypeName)) return;

            for (var i = 0; i < _amount; i++)
            {
                _storage.Insert(Owner.EntityManager.SpawnEntity(_prototypeName, Owner.Transform.Coordinates));
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            if (_appearanceComponent != null)
            {
                switch (message)
                {
                    case ContainerContentsModifiedMessage msg:
                        var actual = Count(msg.Container.ContainedEntities);
                        _appearanceComponent.SetData(StackVisuals.Actual, actual);
                        _appearanceComponent.SetData(StackVisuals.MaxCount, _amount);
                        break;
                }
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
        }

        protected override void Shutdown()
        {
            base.Shutdown();
        }

        private int Count(IReadOnlyList<IEntity> containerContainedEntities)
        {
            var count = 0;
            foreach (var entity in containerContainedEntities)
            {
                if (entity.Prototype != null && entity.Prototype.ID.Equals(_prototypeName))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
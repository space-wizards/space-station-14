using System.Collections.Generic;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Items.Storage.Fill
{
    [RegisterComponent]
    internal sealed class StorageFillComponent : Component, IMapInit
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Name => "StorageFill";

        [ViewVariables]
        private List<string> _contents = new List<string>();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _contents, "contents", new List<string>());
        }

        void IMapInit.MapInit()
        {
            if (_contents.Count == 0)
            {
                return;
            }

            var storage = Owner.GetComponent<IStorageComponent>();

            void Spawn(string prototype)
            {
                storage.Insert(_entityManager.SpawnEntity(prototype, Owner.Transform.Coordinates));
            }

            foreach (var prototype in _contents)
            {
                Spawn(prototype);
            }
        }
    }
}

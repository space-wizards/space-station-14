using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class VisualizerDataInt : IGraphAction, ISerializationHooks
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        void ISerializationHooks.AfterDeserialization()
        {
            IoCManager.InjectDependencies(this);
        }

        [DataField("key")] public string Key { get; private set; } = string.Empty;
        [DataField("data")] public int Data { get; private set; } = 0;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (string.IsNullOrEmpty(Key)) return;

            if (entity.TryGetComponent(out AppearanceComponent? appearance))
            {
                if(_reflectionManager.TryParseEnumReference(Key, out var @enum))
                {
                    appearance.SetData(@enum, Data);
                }
                else
                {
                    appearance.SetData(Key, Data);
                }
            }
        }
    }
}

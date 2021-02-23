#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class VisualizerDataInt : IGraphAction
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        public VisualizerDataInt()
        {
            IoCManager.InjectDependencies(this);
        }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Key, "key", string.Empty);
            serializer.DataField(this, x => x.Data, "data", 0);
        }

        public string? Key { get; private set; } = string.Empty;
        public int Data { get; private set; } = 0;

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

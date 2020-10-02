using System;
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class VisualizerDataInt : IEdgeCompleted, IStepCompleted
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        public VisualizerDataInt()
        {
            IoCManager.InjectDependencies(this);
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Key, "key", null);
            serializer.DataField(this, x => x.Data, "data", 0);
        }

        public string Key { get; private set; }
        public int Data { get; private set; }

        public async Task StepCompleted(IEntity entity, IEntity user)
        {
            await Completed(entity, user);
        }

        public async Task Completed(IEntity entity, IEntity user)
        {
            if (entity.TryGetComponent(out AppearanceComponent appearance))
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

using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class VisualizerDataInt : IGraphAction, ISerializationHooks
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        [DataField("key")] public string Key { get; private set; } = string.Empty;
        [DataField("data")] public int Data { get; private set; } = 0;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (string.IsNullOrEmpty(Key)) return;

            if (entityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                if (IoCManager.Resolve<IReflectionManager>().TryParseEnumReference(Key, out var @enum))
                {
                    _appearance.SetData(uid, @enum, Data, appearance);
                }
            }
        }
    }
}

using System.Threading.Tasks;
using Content.Server.Construction.Components;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class AddContainer : IGraphAction
    {
        [DataField("container")] public string? Container { get; private set; } = null;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (string.IsNullOrEmpty(Container))
                return;

            entityManager.EntitySysManager.GetEntitySystem<ConstructionSystem>().AddContainer(uid, Container);
        }
    }
}

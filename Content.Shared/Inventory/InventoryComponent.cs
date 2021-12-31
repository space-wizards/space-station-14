using System.Linq;
using Content.Shared.Acts;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Inventory;

public class InventoryComponent : Component, IExAct
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public sealed override string Name => "Inventory";

    [DataField("templateId", required: true,
        customTypeSerializer: typeof(PrototypeIdSerializer<InventoryTemplatePrototype>))]
    public string TemplateId { get; } = "human";

    void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
    {
        if (eventArgs.Severity < ExplosionSeverity.Heavy)
        {
            return;
        }

        if (EntitySystem.Get<InventorySystem>()
            .TryGetContainerSlotEnumerator(Owner, out var enumerator, this))
        {
            while (enumerator.MoveNext(out var container))
            {
                if (!container.ContainedEntity.HasValue) continue;
                foreach (var exAct in _entityManager.GetComponents<IExAct>(container.ContainedEntity.Value).ToArray())
                {
                    exAct.OnExplosion(eventArgs);
                }
            }
        }
    }
}

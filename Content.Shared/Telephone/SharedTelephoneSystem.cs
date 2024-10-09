using System.Linq;

namespace Content.Shared.Telephone;

public abstract class SharedTelephoneSystem : EntitySystem
{
    public bool IsTelephoneEngaged(Entity<TelephoneComponent> entity)
    {
        return entity.Comp.LinkedTelephones.Any();
    }
}

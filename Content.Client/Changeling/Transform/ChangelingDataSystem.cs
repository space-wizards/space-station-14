using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Changeling.Transform;

public sealed class ChangelingDataSystem : EntitySystem
{
    public EntityUid getIdentityEntity(NetEntity entity)
    {
        return GetEntity(entity);
    }
}

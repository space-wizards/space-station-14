using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Tabletop
{
    [ImplicitDataDefinitionForInheritors]
    public abstract class TabletopSetup
    {
        public abstract void SetupTabletop(TabletopSession session, IEntityManager entityManager);
    }
}

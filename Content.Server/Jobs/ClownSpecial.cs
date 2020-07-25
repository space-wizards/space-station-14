using Content.Server.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Jobs
{
    // Used by clown job def.
    [UsedImplicitly]
    public sealed class ClownSpecial : JobSpecial
    {
        public override void AfterEquip(IEntity mob)
        {
            base.AfterEquip(mob);

            mob.AddComponent<ClumsyComponent>();
        }
    }
}

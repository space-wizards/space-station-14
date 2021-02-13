using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Provides special hooks for when jobs get spawned in/equipped.
    /// </summary>
    public abstract class JobSpecial : IExposeData
    {
        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            ExposeData(serializer);
        }

        protected virtual void ExposeData(ObjectSerializer serializer)
        {
        }

        public virtual void AfterEquip(IEntity mob)
        {

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Mobs;
using SS14.Server.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Log;

namespace Content.Server.GameObjects.Components.Mobs
{
    /// <summary>
    ///     Stores a <see cref="Server.Mobs.Mind"/> on a mob.
    /// </summary>
    public class MindComponent : Component
    {
        /// <inheritdoc />
        public override string Name => "Mind";

        /// <summary>
        ///     The mind controlling this mob. Can be null.
        /// </summary>
        public Mind Mind { get; private set; }

        /// <summary>
        ///     True if we have a mind, false otherwise.
        /// </summary>
        public bool HasMind => Mind != null;

        /// <summary>
        ///     Don't call this unless you know what the hell you're doing.
        ///     Use <see cref="Mind.TransferTo(IEntity)"/> instead.
        ///     If that doesn't cover it, make something to cover it.
        /// </summary>
        public void InternalEjectMind()
        {
            Mind = null;
        }

        /// <summary>
        ///     Don't call this unless you know what the hell you're doing.
        ///     Use <see cref="Mind.TransferTo(IEntity)"/> instead.
        ///     If that doesn't cover it, make something to cover it.
        /// </summary>
        public void InternalAssignMind(Mind value)
        {
            Mind = value;
        }

        public override void OnRemove()
        {
            base.OnRemove();

            if (HasMind)
            {
                Mind.TransferTo(null);
            }
        }
    }
}

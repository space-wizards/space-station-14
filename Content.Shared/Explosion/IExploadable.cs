using Content.Shared.Acts;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using System;

namespace Content.Shared.Explosion
{
    [Obsolete]
    public interface IExploadable
    {
        /// <summary>
        /// Called when explosion reaches the entity
        /// </summary>
        void OnExplosion(ExplosionEventArgs eventArgs);
    }

    [Obsolete]
    public class ExplosionEventArgs : EventArgs
    {
        public EntityCoordinates Source { get; set; }
        public IEntity Target { get; set; } = default!;
        public ExplosionSeverity Severity { get; set; }
    }

    /// <summary>
    ///     Raised when an explosion reaches an IExploadable
    /// </summary>
    [PublicAPI]
    public class ExplosionEvent : EntityEventArgs
    {
        public EntityCoordinates Source { get; set; }
        public IEntity Target { get; set; } = default!;
        public ExplosionSeverity Severity { get; set; }
    }
}

using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Storage.Visualizers
{
    [UsedImplicitly]
    public sealed class StorageVisualizer : AppearanceVisualizer
    {
        /// <summary>
        /// Sets the base sprite to this layer. Exists to make the inheritance tree less boilerplate-y.
        /// </summary>
        [DataField("state")]
        private string? _stateBase;
        [DataField("state_alt")]
        private string? _stateBaseAlt;
        [DataField("state_open")]
        private string? _stateOpen;
        [DataField("state_closed")]
        private string? _stateClosed;

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out SpriteComponent? sprite))
            {
                return;
            }

            if (_stateBase != null)
            {
                sprite.LayerSetState(0, _stateBase);
            }

            if (_stateBaseAlt == null)
            {
                _stateBaseAlt = _stateBase;
            }
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out SpriteComponent? sprite))
            {
                return;
            }

            component.TryGetData(StorageVisuals.Open, out bool open);

            if (sprite.LayerMapTryGet(StorageVisualLayers.Door, out _))
            {
                sprite.LayerSetVisible(StorageVisualLayers.Door, true);

                if (open)
                {
                    if (_stateOpen != null)
                    {
                        sprite.LayerSetState(StorageVisualLayers.Door, _stateOpen);
                        sprite.LayerSetVisible(StorageVisualLayers.Door, true);
                    }

                    if (_stateBaseAlt != null)
                        sprite.LayerSetState(0, _stateBaseAlt);
                }
                else if (!open)
                {
                    if (_stateClosed != null)
                        sprite.LayerSetState(StorageVisualLayers.Door, _stateClosed);
                    else
                    {
                        sprite.LayerSetVisible(StorageVisualLayers.Door, false);
                    }

                    if (_stateBase != null)
                        sprite.LayerSetState(0, _stateBase);
                }
                else
                {
                    sprite.LayerSetVisible(StorageVisualLayers.Door, false);
                }
            }

            if (component.TryGetData(StorageVisuals.CanLock, out bool canLock) && canLock)
            {
                if (!component.TryGetData(StorageVisuals.Locked, out bool locked))
                {
                    locked = true;
                }

                sprite.LayerSetVisible(StorageVisualLayers.Lock, !open);
                if (!open)
                {
                    sprite.LayerSetState(StorageVisualLayers.Lock, locked ? "locked" : "unlocked");
                }
            }
        }
    }

    public enum StorageVisualLayers : byte
    {
        Door,
        Lock
    }
}

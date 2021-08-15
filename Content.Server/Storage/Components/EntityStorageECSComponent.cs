using Content.Shared.Physics;
using Content.Shared.Sound;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.Storage.Components
{
    [RegisterComponent]
    public class EntityStorageECSComponent : Component
    {
        public override string Name => "EntityStorageECS";

        /// <summary>
        ///  maximum width or height of an entity allowed inside the storage.
        /// </summary>
        public const float MaxSize = 1.0f;

        public const int OpenMask = (int) (
    CollisionGroup.MobImpassable |
    CollisionGroup.VaultImpassable |
    CollisionGroup.SmallImpassable);

        public TimeSpan LastInternalOpenAttempt { get; set; }

        [ViewVariables]
        [DataField("capacity")]
        public int StorageCapacityMax { get; set; } = 30;

        [ViewVariables]
        [DataField("isCollidableWhenOpen")]
        public bool IsCollidableWhenOpen { get; set; }

        [DataField("showContents")]
        private bool _showContents;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ShowContents
        {
            get => _showContents;
            set
            {
                _showContents = value;
                Contents.ShowContents = value;
            }

        }

        [DataField("occludesLight")]
        private bool _occludesLight = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool OccludesLight
        {
            get => _occludesLight;
            set
            {
                _occludesLight = value;
                Contents.OccludesLight = value;
            }
        }

        [DataField("open")]
        public bool Open { get; set; }

        [DataField("canWeldShut")]
        private bool _canWeldShut = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanWeldShut
        {
            get => _canWeldShut;
            set
            {
                if (_canWeldShut == value) return;

                _canWeldShut = value;
            }
        }

        [DataField("isWeldedShut")]
        private bool _isWeldedShut;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsWeldedShut
        {
            get => _isWeldedShut;
            set
            {
                if (_isWeldedShut == value) return;

                _isWeldedShut = value;
            }
        }

        [DataField("closeSound")]
        public SoundSpecifier CloseSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/closetclose.ogg");

        [DataField("openSound")]
        public SoundSpecifier OpenSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/closetopen.ogg");

        [ViewVariables]
        public Container Contents { get; set; } = default!;

        public bool BeingWelded { get; set; }
    }
}

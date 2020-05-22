// Only unused on .NET Core due to KeyValuePair.Deconstruct
// ReSharper disable once RedundantUsingDirective

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
{
    [RegisterComponent]
    public class ToolComponent : SharedToolComponent
    {
#pragma warning disable 649
        [Dependency] private IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IRobustRandom _robustRandom;
#pragma warning restore 649

        private AudioSystem _audioSystem;
        private InteractionSystem _interactionSystem;
        private SpriteComponent _spriteComponent;

        protected ToolQuality _qualities = ToolQuality.Anchoring;

        [ViewVariables]
        public override ToolQuality Qualities
        {
            get => _qualities;
            set
            {
                _qualities = value;
                Dirty();
            }
        }

        /// <summary>
        ///     For tool interactions that have a delay before action this will modify the rate, time to wait is divided by this value
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SpeedModifier { get; set; } = 1;

        public string UseSound { get; set; }

        public string UseSoundCollection { get; set; }

        public void AddQuality(ToolQuality quality)
        {
            _qualities |= quality;
            Dirty();
        }

        public void RemoveQuality(ToolQuality quality)
        {
            _qualities &= ~quality;
            Dirty();
        }

        public bool HasQuality(ToolQuality quality)
        {
            return _qualities.HasFlag(quality);
        }

        public override void Initialize()
        {
            base.Initialize();

            _audioSystem = _entitySystemManager.GetEntitySystem<AudioSystem>();
            _interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
            Owner.TryGetComponent(out _spriteComponent);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            if (serializer.Reading)
            {
                var qualities = serializer.ReadDataField("qualities", new List<ToolQuality>());
                foreach (var quality in qualities)
                {
                    AddQuality(quality);
                }
            }
            serializer.DataField(this, mod => SpeedModifier, "speed", 1);
            serializer.DataField(this, use => UseSound, "useSound", string.Empty);
            serializer.DataField(this, collection => UseSoundCollection, "useSoundCollection", string.Empty);
        }

        public virtual bool UseTool(IEntity user, IEntity target, ToolQuality toolQualityNeeded)
        {
            if (!HasQuality(toolQualityNeeded) || !ActionBlockerSystem.CanInteract(user))
                return false;

            PlayUseSound();

            return true;
        }

        protected void PlaySoundCollection(string name, float volume=-5f)
        {
            var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(name);
            var file = _robustRandom.Pick(soundCollection.PickFiles);
            _entitySystemManager.GetEntitySystem<AudioSystem>()
                .Play(file, Owner, AudioHelpers.WithVariation(0.15f).WithVolume(volume));
        }

        public void PlayUseSound(float volume=-5f)
        {
            if(string.IsNullOrEmpty(UseSoundCollection))
                _audioSystem.Play(UseSound, Owner, AudioHelpers.WithVariation(0.15f).WithVolume(volume));
            else
                PlaySoundCollection(UseSoundCollection, volume);
        }
    }
}

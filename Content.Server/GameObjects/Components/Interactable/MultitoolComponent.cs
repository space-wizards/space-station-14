using System.Collections.Generic;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Interactable
{
    /// <summary>
    ///     Not to be confused with Multitool (power)
    /// </summary>
    [RegisterComponent]
    public class MultiToolComponent : Component, IUse
    {
        public class ToolEntry : IExposeData
        {
            private string _state;
            private string _sound;
            private string _soundCollection;
            private string _texture;
            private string _sprite;
            private string _changeSound;

            public ToolQuality Behavior { get; private set; }
            public string State => _state;
            public string Texture => _texture;
            public string Sprite => _sprite;
            public string Sound => _sound;
            public string SoundCollection => _soundCollection;
            public string ChangeSound => _changeSound;

            void IExposeData.ExposeData(ObjectSerializer serializer)
            {
                serializer.DataField(this, x => x.Behavior, "behavior", ToolQuality.None);
                serializer.DataField(ref _state, "state", string.Empty);
                serializer.DataField(ref _sprite, "sprite", string.Empty);
                serializer.DataField(ref _texture, "texture", string.Empty);
                serializer.DataField(ref _sound, "useSound", string.Empty);
                serializer.DataField(ref _soundCollection, "useSoundCollection", string.Empty);
                serializer.DataField(ref _changeSound, "changeSound", string.Empty);
            }
        }

        public override string Name => "MultiTool";
        public override uint? NetID => ContentNetIDs.MULTITOOLS;
        private List<ToolEntry> _tools;
        private int _currentTool = 0;

        private AudioSystem _audioSystem;
        private ToolComponent _tool;
        private SpriteComponent _sprite;

        public override void Initialize()
        {
            base.Initialize();
            Owner.TryGetComponent(out _tool);
            Owner.TryGetComponent(out _sprite);

            _audioSystem = EntitySystem.Get<AudioSystem>();

            SetTool();
        }

        public void Cycle()
        {
            _currentTool = (_currentTool + 1) % _tools.Count;
            SetTool();
            var current = _tools[_currentTool];
            if(!string.IsNullOrEmpty(current.ChangeSound))
                _audioSystem.PlayFromEntity(current.ChangeSound, Owner);
        }

        private void SetTool()
        {
            if (_tool == null) return;

            var current = _tools[_currentTool];

            _tool.UseSound = current.Sound;
            _tool.UseSoundCollection = current.SoundCollection;
            _tool.Qualities = current.Behavior;

            if (_sprite == null) return;

            if (string.IsNullOrEmpty(current.Texture))
                if (!string.IsNullOrEmpty(current.Sprite))
                    _sprite.LayerSetState(0, current.State, current.Sprite);
                else
                    _sprite.LayerSetState(0, current.State);
            else
                _sprite.LayerSetTexture(0, current.Texture);

            Dirty();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _tools, "tools", new List<ToolEntry>());
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            Cycle();
            return true;
        }

        public override ComponentState GetComponentState()
        {
            return new MultiToolComponentState(_tool.Qualities);
        }
    }
}

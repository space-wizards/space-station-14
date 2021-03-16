using System.Collections.Generic;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Interactable
{
    /// <summary>
    ///     Not to be confused with Multitool (power)
    /// </summary>
    [RegisterComponent]
    public class MultiToolComponent : Component, IUse
    {
        [DataDefinition]
        public class ToolEntry
        {
            [DataField("behavior")] public ToolQuality Behavior { get; private set; } = ToolQuality.None;

            [field: DataField("state")]
            public string State { get; } = string.Empty;

            [field: DataField("texture")]
            public string Texture { get; } = string.Empty;

            [field: DataField("sprite")]
            public string Sprite { get; } = string.Empty;

            [field: DataField("useSound")]
            public string Sound { get; } = string.Empty;

            [field: DataField("useSoundCollection")]
            public string SoundCollection { get; } = string.Empty;

            [field: DataField("changeSound")]
            public string ChangeSound { get; } = string.Empty;
        }

        public override string Name => "MultiTool";
        public override uint? NetID => ContentNetIDs.MULTITOOLS;
        [DataField("tools")] private List<ToolEntry> _tools = new();
        private int _currentTool = 0;

        private AudioSystem _audioSystem = default!;
        private ToolComponent? _tool;
        private SpriteComponent? _sprite;

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

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            Cycle();
            return true;
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new MultiToolComponentState(_tool?.Qualities ?? ToolQuality.None);
        }
    }
}

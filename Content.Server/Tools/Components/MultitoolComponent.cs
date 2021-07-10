using System.Collections.Generic;
using Content.Shared.Interaction;
using Content.Shared.NetIDs;
using Content.Shared.Sound;
using Content.Shared.Tool;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Tools.Components
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

            [DataField("state")]
            public string State { get; } = string.Empty;

            [DataField("texture")]
            public string Texture { get; } = string.Empty;

            [DataField("sprite")]
            public string Sprite { get; } = string.Empty;

            [DataField("useSound")]
            public SoundSpecifier Sound { get; } = default!;

            [DataField("changeSound")]
            public SoundSpecifier ChangeSound { get; } = default!;
        }

        public override string Name => "MultiTool";
        public override uint? NetID => ContentNetIDs.MULTITOOLS;
        [DataField("tools")] private List<ToolEntry> _tools = new();
        private int _currentTool = 0;

        private ToolComponent? _tool;
        private SpriteComponent? _sprite;

        protected override void Initialize()
        {
            base.Initialize();
            Owner.TryGetComponent(out _tool);
            Owner.TryGetComponent(out _sprite);
            SetTool();
        }

        public void Cycle()
        {
            _currentTool = (_currentTool + 1) % _tools.Count;
            SetTool();
            var current = _tools[_currentTool];
            if(current.ChangeSound.TryGetSound(out var changeSound))
                SoundSystem.Play(Filter.Pvs(Owner), changeSound, Owner);
        }

        private void SetTool()
        {
            if (_tool == null) return;

            var current = _tools[_currentTool];

            _tool.UseSound = current.Sound;
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

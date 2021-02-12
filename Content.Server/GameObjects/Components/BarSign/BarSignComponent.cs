#nullable enable
using System.Linq;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.BarSign
{
    [RegisterComponent]
    public class BarSignComponent : Component, IMapInit
    {
        public override string Name => "BarSign";

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        private string? _currentSign;

        [ViewVariables(VVAccess.ReadWrite)]
        public string? CurrentSign
        {
            get => _currentSign;
            set
            {
                _currentSign = value;
                UpdateSignInfo();
            }
        }

        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        private void UpdateSignInfo()
        {
            if (_currentSign == null)
            {
                return;
            }

            if (!_prototypeManager.TryIndex(_currentSign, out BarSignPrototype prototype))
            {
                Logger.ErrorS("barSign", $"Invalid bar sign prototype: \"{_currentSign}\"");
                return;
            }

            if (Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                if (!Powered)
                {
                    sprite.LayerSetState(0, "empty");
                    sprite.LayerSetShader(0, "shaded");
                }
                else
                {
                    sprite.LayerSetState(0, prototype.Icon);
                    sprite.LayerSetShader(0, "unshaded");
                }
            }

            if (!string.IsNullOrEmpty(prototype.Name))
            {
                Owner.Name = prototype.Name;
            }
            else
            {
                Owner.Name = Loc.GetString("bar sign");
            }

            Owner.Description = prototype.Description;
        }

        public override void Initialize()
        {
            base.Initialize();

            UpdateSignInfo();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    PowerOnOnPowerStateChanged(powerChanged);
                    break;
            }
        }

        private void PowerOnOnPowerStateChanged(PowerChangedMessage e)
        {
            UpdateSignInfo();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _currentSign, "current", null);
        }

        public void MapInit()
        {
            if (_currentSign != null)
            {
                return;
            }

            var prototypes = _prototypeManager.EnumeratePrototypes<BarSignPrototype>().Where(p => !p.Hidden)
                .ToList();
            var prototype = _robustRandom.Pick(prototypes);

            CurrentSign = prototype.ID;
        }
    }
}

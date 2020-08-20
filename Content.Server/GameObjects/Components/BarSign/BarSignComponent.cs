#nullable enable
using System.Linq;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
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

#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
#pragma warning restore 649

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

        private bool Powered => PowerReceiver == null || PowerReceiver.Powered;

        private PowerReceiverComponent? PowerReceiver =>
            Owner.TryGetComponent(out PowerReceiverComponent? receiver) ? receiver : null;

        private SpriteComponent? Sprite => Owner.TryGetComponent(out SpriteComponent? sprite) ? sprite : null;

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

            if (!Powered)
            {
                Sprite?.LayerSetState(0, "empty");
                Sprite?.LayerSetShader(0, "shaded");
            }
            else
            {
                Sprite?.LayerSetState(0, prototype.Icon);
                Sprite?.LayerSetShader(0, "unshaded");
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

            if (PowerReceiver != null)
            {
                PowerReceiver.OnPowerStateChanged += PowerOnOnPowerStateChanged;
            }

            UpdateSignInfo();
        }

        public override void OnRemove()
        {
            if (PowerReceiver != null)
            {
                PowerReceiver.OnPowerStateChanged -= PowerOnOnPowerStateChanged;
            }

            base.OnRemove();
        }

        private void PowerOnOnPowerStateChanged(object? sender, PowerStateEventArgs e)
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

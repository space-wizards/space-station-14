#nullable enable
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Items.Clothing;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Atmos.Jetpack;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Jetpack
{
    [RegisterComponent]
    public class JetpackComponent : Component
    {
        public override string Name => "Jetpack";

        [ViewVariables(VVAccess.ReadWrite)]
        public float VolumeUsage { get; set; } = Atmospherics.BreathVolume;

        [ViewVariables]
        [ComponentDependency]
        private readonly GasTankComponent? _gasTank = null;

        [ComponentDependency]
        private readonly SpriteComponent? _sprite = null;
        [ComponentDependency]
        private readonly ClothingComponent? _clothing = null;

        private bool _active = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                var state = "icon" + (Active ? "-on" : "");
                _sprite?.LayerSetState(0, state);
                if (_clothing != null)
                    _clothing.ClothingEquippedPrefix = Active ? "on" : null;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public void Update()
        {
            if (!Active)
                return;
            //TODO: remove only on walk
            if (_gasTank != null && _gasTank.Air != null)
            {
                _gasTank.RemoveAirVolume(VolumeUsage);
                if (_gasTank.Air.Pressure <= VolumeUsage)
                    Active = false;
            }
            else
            {
                Active = false;
            }
        }

        public void ToggleJetpack()
        {
            if (_gasTank != null && _gasTank.Air != null)
            {
                if (_gasTank.Air.Pressure <= VolumeUsage)
                    return;
            }
            Active = !Active;
        }
    }

    [UsedImplicitly]
    public class ToggleJetpackAction : IToggleItemAction
    {
        public void ExposeData(ObjectSerializer serializer) {}

        public bool DoToggleAction(ToggleItemActionEventArgs args)
        {
            if (!args.Item.TryGetComponent<JetpackComponent>(out var jetpackComponent)) return false;
            // no change
            if (jetpackComponent.Active == args.ToggledOn) return false;
            jetpackComponent.ToggleJetpack();
            // did we successfully toggle to the desired status?
            return jetpackComponent.Active == args.ToggledOn;
        }
    }
}

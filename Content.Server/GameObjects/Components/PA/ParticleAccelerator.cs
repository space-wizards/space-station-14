using System.Linq;
using System.Threading;
using Content.Server.Atmos;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.PA
{
    //todo remove components when they get deleted

    public class ParticleAccelerator
    {
        private IEntityManager _entityManager;
        private IMapManager _mapManager;
        public bool SetForDeconstruct;

        public ParticleAccelerator()
        {
            _entityManager = IoCManager.Resolve<IEntityManager>();
            _mapManager = IoCManager.Resolve<IMapManager>();
        }

        private EntityUid? _EntityId;

        [ViewVariables]
        private ParticleAcceleratorControlBoxComponent _controlBox;
        public ParticleAcceleratorControlBoxComponent ControlBox
        {
            get => _controlBox;
            set => SetControlBox(value);
        }
        private void SetControlBox(ParticleAcceleratorControlBoxComponent value, bool skipFuelChamberCheck = false)
        {
            if(!TryAddPart(ref _controlBox, value, out var gridId)) return;

            if (!skipFuelChamberCheck &&
                TryGetPart<ParticleAcceleratorFuelChamberComponent>(gridId, PartOffset.Right, value, out var fuelChamber))
            {
                SetFuelChamber(fuelChamber, skipControlBoxCheck: true);
            }

            Power = _power;
        }

        [ViewVariables]
        private ParticleAcceleratorEndCapComponent _endCap;
        public ParticleAcceleratorEndCapComponent EndCap
        {
            get => _endCap;
            set => SetEndCap(value);
        }
        private void SetEndCap(ParticleAcceleratorEndCapComponent value, bool skipFuelChamberCheck = false)
        {
            if(!TryAddPart(ref _endCap, value, out var gridId)) return;
            if (!skipFuelChamberCheck &&
                TryGetPart<ParticleAcceleratorFuelChamberComponent>(gridId, PartOffset.Down, value, out var fuelChamber))
            {
                SetFuelChamber(fuelChamber, skipEndCapCheck: true);
            }

            Power = _power;
        }

        [ViewVariables]
        private ParticleAcceleratorFuelChamberComponent _fuelChamber;
        public ParticleAcceleratorFuelChamberComponent FuelChamber
        {
            get => _fuelChamber;
            set => SetFuelChamber(value);
        }
        private void SetFuelChamber(ParticleAcceleratorFuelChamberComponent value, bool skipEndCapCheck = false, bool skipPowerBoxCheck = false, bool skipControlBoxCheck = false)
        {
            if(!TryAddPart(ref _fuelChamber, value, out var gridId)) return;

            if (!skipControlBoxCheck &&
                TryGetPart<ParticleAcceleratorControlBoxComponent>(gridId, PartOffset.Left, value, out var controlBox))
            {
                SetControlBox(controlBox, skipFuelChamberCheck: true);
            }

            if (!skipEndCapCheck &&
                TryGetPart<ParticleAcceleratorEndCapComponent>(gridId, PartOffset.Up, value, out var endCap))
            {
                SetEndCap(endCap, skipFuelChamberCheck: true);
            }

            if (!skipPowerBoxCheck &&
                TryGetPart<ParticleAcceleratorPowerBoxComponent>(gridId, PartOffset.Down, value, out var powerBox))
            {
                SetPowerBox(powerBox, skipFuelChamberCheck: true);
            }

            Power = _power;
        }

        [ViewVariables]
        private ParticleAcceleratorPowerBoxComponent _powerBox;
        public ParticleAcceleratorPowerBoxComponent PowerBox
        {
            get => _powerBox;
            set => SetPowerBox(value);
        }
        private void SetPowerBox(ParticleAcceleratorPowerBoxComponent value, bool skipFuelChamberCheck = false,
            bool skipEmitterCenterCheck = false)
        {
            if(!TryAddPart(ref _powerBox, value, out var gridId)) return;

            if (!skipFuelChamberCheck &&
                TryGetPart<ParticleAcceleratorFuelChamberComponent>(gridId, PartOffset.Up, value, out var fuelChamber))
            {
                SetFuelChamber(fuelChamber, skipPowerBoxCheck: true);
            }

            if (!skipEmitterCenterCheck && TryGetPart(gridId, PartOffset.Down, value,
                ParticleAcceleratorEmitterType.Center, out var emitterComponent))
            {
                SetEmitterCenter(emitterComponent, skipPowerBoxCheck: true);
            }

            Power = _power;
        }

        [ViewVariables]
        private ParticleAcceleratorEmitterComponent _emitterLeft;
        public ParticleAcceleratorEmitterComponent EmitterLeft
        {
            get => _emitterLeft;
            set => SetEmitterLeft(value);
        }
        private void SetEmitterLeft(ParticleAcceleratorEmitterComponent value, bool skipEmitterCenterCheck = false)
        {
            if (value != null && value.Type != ParticleAcceleratorEmitterType.Left)
            {
                Logger.Error($"Something tried adding a left Emitter that doesn't have the Emittertype left to a ParticleAccelerator (Actual Emittertype: {value.Type})");
                return;
            }

            if(!TryAddPart(ref _emitterLeft, value, out var gridId)) return;

            if (!skipEmitterCenterCheck && TryGetPart(gridId, PartOffset.Right, value,
                ParticleAcceleratorEmitterType.Center, out var emitterComponent))
            {
                SetEmitterCenter(emitterComponent, skipEmitterLeftCheck: true);
            }

            Power = _power;
        }

        [ViewVariables]
        private ParticleAcceleratorEmitterComponent _emitterCenter;
        public ParticleAcceleratorEmitterComponent EmitterCenter
        {
            get => _emitterCenter;
            set => SetEmitterCenter(value);
        }
        private void SetEmitterCenter(ParticleAcceleratorEmitterComponent value, bool skipEmitterLeftCheck = false,
            bool skipEmitterRightCheck = false, bool skipPowerBoxCheck = false)
        {
            if (value != null && value.Type != ParticleAcceleratorEmitterType.Center)
            {
                Logger.Error($"Something tried adding a center Emitter that doesn't have the Emittertype center to a ParticleAccelerator (Actual Emittertype: {value.Type})");
                return;
            }

            if(!TryAddPart(ref _emitterCenter, value, out var gridId)) return;

            if (!skipEmitterLeftCheck && TryGetPart(gridId, PartOffset.Left, value, ParticleAcceleratorEmitterType.Left,
                out var emitterLeft))
            {
                SetEmitterLeft(emitterLeft, skipEmitterCenterCheck: true);
            }

            if (!skipEmitterRightCheck && TryGetPart(gridId, PartOffset.Right, value,
                ParticleAcceleratorEmitterType.Right,
                out var emitterRight))
            {
                SetEmitterRight(emitterRight, skipEmitterCenterCheck: true);
            }

            if (!skipPowerBoxCheck &&
                TryGetPart<ParticleAcceleratorPowerBoxComponent>(gridId, PartOffset.Up, value, out var powerBox))
            {
                SetPowerBox(powerBox, skipEmitterCenterCheck: true);
            }

            Power = _power;
        }

        [ViewVariables]
        private ParticleAcceleratorEmitterComponent _emitterRight;
        public ParticleAcceleratorEmitterComponent EmitterRight
        {
            get => _emitterRight;
            set => SetEmitterRight(value);
        }
        private void SetEmitterRight(ParticleAcceleratorEmitterComponent value, bool skipEmitterCenterCheck = false)
        {
            if (value != null && value.Type != ParticleAcceleratorEmitterType.Right)
            {
                Logger.Error($"Something tried adding a right Emitter that doesn't have the Emittertype right to a ParticleAccelerator (Actual Emittertype: {value.Type})");
                return;
            }

            if(!TryAddPart(ref _emitterRight, value, out var gridId)) return;

            if (!skipEmitterCenterCheck && TryGetPart(gridId, PartOffset.Left, value,
                ParticleAcceleratorEmitterType.Center, out var emitterComponent))
            {
                SetEmitterCenter(emitterComponent, skipEmitterRightCheck: true);
            }

            Power = _power;
        }

        private ParticleAcceleratorPowerState _power = ParticleAcceleratorPowerState.Standby;
        [ViewVariables(VVAccess.ReadWrite)]
        public ParticleAcceleratorPowerState Power
        {
            get => _power;
            set
            {
                if (!_enabled) return;

                if(_power == value) return;

                _power = value;
                UpdatePartVisualStates();
                _controlBox?.OnParticleAcceleratorValuesChanged();

                UpdateFireLoop();
            }
        }

        private bool _enabled;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;

                _enabled = value && IsFunctional();
                UpdatePartVisualStates();
                _controlBox?.OnParticleAcceleratorValuesChanged();

                UpdateFireLoop();
            }
        }

        private void UpdateFireLoop()
        {
            StopFiring();
            if (_power > ParticleAcceleratorPowerState.Standby && _enabled)
            {
                StartFiring();
            }
        }

        public bool IsFunctional()
        {
            return ControlBox != null && EndCap != null && FuelChamber != null && PowerBox != null &&
                   EmitterCenter != null && EmitterLeft != null && EmitterRight != null;
        }

        private void UpdatePartVisualStates()
        {
            UpdatePartVisualState(ControlBox);
            UpdatePartVisualState(EndCap);
            UpdatePartVisualState(FuelChamber);
            UpdatePartVisualState(PowerBox);
            UpdatePartVisualState(EmitterCenter);
            UpdatePartVisualState(EmitterLeft);
            UpdatePartVisualState(EmitterRight);
        }

        private void UpdatePartVisualState(ParticleAcceleratorPartComponent component)
        {
            if (component?.Owner == null) return;

            if (!component.Owner.TryGetComponent<AppearanceComponent>(out var appearanceComponent))
            {
                Logger.Error($"ParticleAccelerator tried updating state of {component} but failed due to a missing AppearanceComponent");
                return;
            }
            appearanceComponent.SetData(ParticleAcceleratorVisuals.VisualState, Enabled ? (ParticleAcceleratorVisualState)_power : ParticleAcceleratorVisualState.Closed);
        }

        public ParticleAcceleratorDataUpdateMessage DataMessage =>
            new ParticleAcceleratorDataUpdateMessage(IsFunctional(),
                Enabled, Power, 0, EmitterLeft != null,
                EmitterCenter != null, EmitterRight != null,
                PowerBox != null, FuelChamber != null,
                EndCap != null);

        private void Absorb(ParticleAccelerator particleAccelerator)
        {
            if (particleAccelerator.SetForDeconstruct) return;

            _controlBox ??= particleAccelerator._controlBox;
            if (_controlBox != null) _controlBox.ParticleAccelerator = this;
            _endCap ??= particleAccelerator._endCap;
            if (_endCap != null) _endCap.ParticleAccelerator = this;
            _fuelChamber ??= particleAccelerator._fuelChamber;
            if (_fuelChamber != null) _fuelChamber.ParticleAccelerator = this;
            _powerBox ??= particleAccelerator._powerBox;
            if (_powerBox != null) _powerBox.ParticleAccelerator = this;
            _emitterLeft ??= particleAccelerator._emitterLeft;
            if (_emitterLeft != null) _emitterLeft.ParticleAccelerator = this;
            _emitterCenter ??= particleAccelerator._emitterCenter;
            if (_emitterCenter != null) _emitterCenter.ParticleAccelerator = this;
            _emitterRight ??= particleAccelerator._emitterRight;
            if (_emitterRight != null) _emitterRight.ParticleAccelerator = this;

            particleAccelerator._controlBox = null;
            particleAccelerator._endCap = null;
            particleAccelerator._fuelChamber = null;
            particleAccelerator._powerBox = null;
            particleAccelerator._emitterLeft = null;
            particleAccelerator._emitterCenter = null;
            particleAccelerator._emitterRight = null;
            if (particleAccelerator._controlBox != null) particleAccelerator._controlBox.ParticleAccelerator = null;
            if (particleAccelerator._endCap != null) particleAccelerator._endCap.ParticleAccelerator = null;
            if (particleAccelerator._fuelChamber != null) particleAccelerator._fuelChamber.ParticleAccelerator = null;
            if (particleAccelerator._powerBox != null) particleAccelerator._powerBox.ParticleAccelerator = null;
            if (particleAccelerator._emitterLeft != null) particleAccelerator._emitterLeft.ParticleAccelerator = null;
            if (particleAccelerator._emitterCenter != null) particleAccelerator._emitterCenter.ParticleAccelerator = null;
            if (particleAccelerator._emitterRight != null) particleAccelerator._emitterRight.ParticleAccelerator = null;

            _controlBox?.OnParticleAcceleratorValuesChanged();
        }

        private CancellationTokenSource _cancellationTokenSource;
        private void StartFiring()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancelToken = _cancellationTokenSource.Token;
            Timer.SpawnRepeating(1000,  () => //todo make speed depend on level?
            {
                EmitterCenter?.Fire();
                EmitterLeft?.Fire();
                _emitterRight?.Fire();
            }, cancelToken);
        }

        private void StopFiring()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }

        private bool TryAddPart<T>(ref T partVar, T value, out GridId gridId) where T : ParticleAcceleratorPartComponent
        {
            gridId = GridId.Invalid;

            if (value != null && value.dontAddToPa) return false;

            if (partVar == value) return false;

            if (value == null)
            {
                SetForDeconstruct = true;
                foreach (var neighbour in partVar.GetNeighbours())
                {
                    neighbour?.RebuildParticleAccelerator();
                }
                partVar = null;
                return false;
            }

            if (partVar != null)
            {
                Logger.Error($"Something tried adding a {value} to a ParticleAccelerator that already has a {partVar} registered");
                return false;
            }

            if (typeof(T) != value.GetType())
            {
                Logger.Error($"Type mismatch when trying to add {partVar} to a ParticleAccelerator");
                return false;
            }

            _EntityId ??= value.Owner.Transform.Coordinates.EntityId;
            if (_EntityId != value.Owner.Transform.Coordinates.EntityId)
            {
                Logger.Error($"Something tried adding a {value} from a different EntityID to a ParticleAccelerator");
                return false;
            }

            gridId = value.Owner.Transform.Coordinates.GetGridId(_entityManager);
            if (gridId == GridId.Invalid)
            {
                Logger.Error($"Something tried adding a {value} that isn't in a Grid to a ParticleAccelerator");
                return false;
            }

            partVar = value;

            if (value.ParticleAccelerator != this)
            {
                Absorb(value.ParticleAccelerator);
                value.ParticleAccelerator = this;
            }

            Validate();

            _controlBox?.OnParticleAcceleratorValuesChanged();

            return true;
        }

        private void Validate()
        {
            Enabled = IsFunctional();
        }

        private bool TryGetPart<TP>(GridId gridId, PartOffset directionOffset, ParticleAcceleratorPartComponent value, out TP part)
            where TP : ParticleAcceleratorPartComponent
        {
            var partMapIndices = GetMapIndicesInDir(value, directionOffset);

            var entity = partMapIndices.GetEntitiesInTileFast(gridId).FirstOrDefault(obj => obj.TryGetComponent<TP>(out var part));
            part = entity?.GetComponent<TP>();
            return entity != null && part != null;
        }

        private bool TryGetPart(GridId gridId, PartOffset directionOffset, ParticleAcceleratorPartComponent value, ParticleAcceleratorEmitterType type, out ParticleAcceleratorEmitterComponent part)
        {
            var partMapIndices = GetMapIndicesInDir(value, directionOffset);

            var entity = partMapIndices.GetEntitiesInTileFast(gridId).FirstOrDefault(obj => obj.TryGetComponent<ParticleAcceleratorEmitterComponent>(out var p) && p.Type == type);
            part = entity?.GetComponent<ParticleAcceleratorEmitterComponent>();
            return entity != null && part != null;
        }

        private MapIndices GetMapIndicesInDir(Component comp, PartOffset offset)
        {
            var offsetAngle = Angle.FromDegrees(180);
            switch (offset)
            {
                case PartOffset.Down:
                    offsetAngle = Angle.FromDegrees(0);
                    break;
                case PartOffset.Left:
                    offsetAngle = Angle.FromDegrees(-90);
                    break;
                case PartOffset.Right:
                    offsetAngle = Angle.FromDegrees(90);
                    break;
            }

            var partDir = new Angle(comp.Owner.Transform.LocalRotation + offsetAngle).GetCardinalDir();
            return comp.Owner.Transform.Coordinates.ToMapIndices(_entityManager, _mapManager).Offset(partDir);
        }

        private enum PartOffset
        {
            Up,
            Down,
            Left,
            Right
        }
    }
}

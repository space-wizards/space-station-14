using System;
using System.Collections.Generic;
using ImGuiNET;
using Robust.Shared.Maths;
using static ImGuiNET.ImGui;
using Color = System.Drawing.Color;
using Vector2 = System.Numerics.Vector2;
using RobustVec2 = Robust.Shared.Maths.Vector2;
using static Content.Server.Power.Pow3r.PowerState;

namespace Pow3r
{
    internal sealed partial class Program
    {
        private bool _showDemo;

        private Dictionary<NodeId, DisplayLoad> _displayLoads = new();
        private Dictionary<NodeId, DisplayBattery> _displayBatteries = new();
        private Dictionary<NodeId, DisplayNetwork> _displayNetworks = new();
        private Dictionary<NodeId, DisplaySupply> _displaySupplies = new();

        private void DoUI(float frameTime)
        {
            if (BeginMainMenuBar())
            {
                _showDemo ^= MenuItem("Demo");
                EndMainMenuBar();
            }

            SetNextWindowSize(new Vector2(150, 200));

            Begin("CreateButtons",
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoResize);

            if (Button("Generator"))
            {
                var supply = new Supply();
                _state.Supplies.Allocate(out supply.Id) = supply;
                _displaySupplies.Add(supply.Id, new DisplaySupply());
            }

            if (Button("Load"))
            {
                var load = new Load();
                _state.Loads.Allocate(out load.Id) = load;
                _displayLoads.Add(load.Id, new DisplayLoad());
            }

            if (Button("Network"))
            {
                var network = new Network();
                _state.Networks.Allocate(out network.Id) = network;
                _state.GroupedNets = null;
                _displayNetworks.Add(network.Id, new DisplayNetwork());
            }

            if (Button("Battery"))
            {
                var battery = new Battery();
                _state.Batteries.Allocate(out battery.Id) = battery;
                _displayBatteries.Add(battery.Id, new DisplayBattery());
                _state.GroupedNets = null;
            }

            Checkbox("Paused", ref _paused);
            SliderInt("TPS", ref _tps, 1, 120);
            SetNextItemWidth(-1);
            Combo("", ref _currentSolver, _solverNames, _solverNames.Length);

            if (Button("Single step"))
                RunSingleStep();

            End();

            Begin("Simulating timing");

            PlotLines("Tick time (ms)", ref _simTickTimes[0], MaxTickData, _tickDataIdx + 1,
                $"{_simTickTimes[_tickDataIdx]:N2}",
                0,
                0.1f, new Vector2(250, 150));

            End();

            Begin("Frame timings");

            PlotLines("Frame (ms)", ref _frameTimings[0], _frameTimings.Length, _frameTimeIdx + 1,
                $"{_frameTimings[_frameTimeIdx]:N2}",
                0,
                33.333f, new Vector2(250, 150));

            End();

            {
                Begin("Memory");

                var heap = GC.GetTotalMemory(false);
                Text($"Managed heap: {heap>>20} MiB");

                End();
            }

            foreach (var network in _state.Networks.Values)
            {
                var displayNetwork = _displayNetworks[network.Id];
                Begin($"Network {network.Id}##Gen{network.Id}");

                Text($"Height: {network.Height}");

                displayNetwork.CurrentWindowPos = CalcWindowCenter();

                if (Button("Delete"))
                {
                    _remQueue.Enqueue(network);

                    if (_linking == network)
                    {
                        _linking = null;
                    }
                }

                SameLine();

                if (_linking != null)
                {
                    if (_linking == network && Button("Cancel"))
                    {
                        _linking = null;
                    }
                }
                else
                {
                    if (Button("Link..."))
                    {
                        _linking = network;
                    }
                }

                End();
            }

            foreach (var load in _state.Loads.Values)
            {
                var displayLoad = _displayLoads[load.Id];

                Begin($"Load {load.Id}##Load{load.Id}");

                Checkbox("Enabled", ref load.Enabled);
                SliderFloat("Desired", ref load.DesiredPower, 0, 1000, "%.0f W");

                displayLoad.CurrentWindowPos = CalcWindowCenter();

                PlotLines("", ref displayLoad.ReceivedPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"Receiving: {load.ReceivingPower:N1} W",
                    0,
                    load.DesiredPower, new Vector2(250, 150));

                if (Button("Delete"))
                {
                    _remQueue.Enqueue(load);
                }

                SameLine();
                if (_linking != null)
                {
                    if (Button("Link"))
                    {
                        _linking.Loads.Add(load.Id);
                        _linking = null;
                        RefreshLinks();
                    }
                }
                else
                {
                    if (load.LinkedNetwork != default && Button("Unlink"))
                    {
                        var net = _state.Networks[load.LinkedNetwork];
                        net.Loads.Remove(load.Id);
                        load.LinkedNetwork = default;
                    }
                }

                End();
            }

            foreach (var supply in _state.Supplies.Values)
            {
                var displaySupply = _displaySupplies[supply.Id];
                Begin($"Generator {supply.Id}##Gen{supply.Id}");

                Checkbox("Enabled", ref supply.Enabled);
                SliderFloat("Available", ref supply.MaxSupply, 0, 1000, "%.0f W");
                SliderFloat("Ramp", ref supply.SupplyRampRate, 0, 100, "%.0f W/s");
                SliderFloat("Tolerance", ref supply.SupplyRampTolerance, 0, 100, "%.0f W");

                displaySupply.CurrentWindowPos = CalcWindowCenter();

                Text($"Ramp Position: {supply.SupplyRampPosition:N1}");

                PlotLines("", ref displaySupply.SuppliedPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"Supply: {supply.CurrentSupply:N1} W",
                    0, supply.MaxSupply, new Vector2(250, 150));

                if (Button("Delete"))
                {
                    _remQueue.Enqueue(supply);
                }

                SameLine();
                if (_linking != null)
                {
                    if (Button("Link"))
                    {
                        _linking.Supplies.Add(supply.Id);
                        _linking = null;
                        RefreshLinks();
                    }
                }
                else
                {
                    if (supply.LinkedNetwork != default && Button("Unlink"))
                    {
                        var net = _state.Networks[supply.LinkedNetwork];
                        net.Supplies.Remove(supply.Id);
                        supply.LinkedNetwork = default;
                    }
                }

                End();
            }

            foreach (var battery in _state.Batteries.Values)
            {
                var displayBattery = _displayBatteries[battery.Id];

                Begin($"Battery {battery.Id}##Bat{battery.Id}");

                Checkbox("Enabled", ref battery.Enabled);
                SliderFloat("Capacity", ref battery.Capacity, 0, 100000, "%.0f J");
                SliderFloat("Max charge rate", ref battery.MaxChargeRate, 0, 1000, "%.0f W");
                SliderFloat("Max supply", ref battery.MaxSupply, 0, 1000, "%.0f W");
                SliderFloat("Ramp", ref battery.SupplyRampRate, 0, 100, "%.0f W/s");
                SliderFloat("Tolerance", ref battery.SupplyRampTolerance, 0, 100, "%.0f W");
                var percent = 100 * battery.Efficiency;
                SliderFloat("Efficiency", ref percent, 0, 100, "%.0f %%");
                battery.Efficiency = percent / 100;

                displayBattery.CurrentWindowPos = CalcWindowCenter();

                SliderFloat("Ramp position", ref battery.SupplyRampPosition, 0, battery.MaxSupply, "%.0f W");

                PlotLines("", ref displayBattery.SuppliedPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"OUT: {battery.CurrentSupply:N1} W",
                    0, battery.MaxSupply + 1000, new Vector2(250, 75));

                PlotLines("", ref displayBattery.ReceivingPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"IN: {battery.CurrentReceiving:N1} W",
                    0, battery.MaxChargeRate + 1000, new Vector2(250, 75));

                PlotLines("", ref displayBattery.StoredPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"Charge: {battery.CurrentStorage:N1} J",
                    0, battery.Capacity, new Vector2(250, 75));

                if (Button("Delete"))
                {
                    _remQueue.Enqueue(battery);
                }

                SameLine();
                if (_linking != null)
                {
                    if (battery.LinkedNetworkCharging == default && Button("Link as load"))
                    {
                        _linking.BatteryLoads.Add(battery.Id);
                        _state.GroupedNets = null;
                        _linking = null;
                        RefreshLinks();
                    }
                    else
                    {
                        SameLine();
                        if (battery.LinkedNetworkDischarging == default && Button("Link as supply"))
                        {
                            _linking.BatterySupplies.Add(battery.Id);
                            _state.GroupedNets = null;
                            _linking = null;
                            RefreshLinks();
                        }
                    }
                }
                else
                {
                    if (battery.LinkedNetworkCharging != default && Button("Unlink loading"))
                    {
                        var net = _state.Networks[battery.LinkedNetworkCharging];
                        net.BatteryLoads.Remove(battery.Id);
                        _state.GroupedNets = null;
                        battery.LinkedNetworkCharging = default;
                    }
                    else
                    {
                        SameLine();
                        if (battery.LinkedNetworkDischarging != default && Button("Unlink supplying"))
                        {
                            var net = _state.Networks[battery.LinkedNetworkDischarging];
                            net.BatterySupplies.Remove(battery.Id);
                            _state.GroupedNets = null;
                            battery.LinkedNetworkDischarging = default;
                        }
                    }
                }

                if (Button("Empty"))
                    battery.CurrentStorage = 0;
                SameLine();
                if (Button("Fill"))
                    battery.CurrentStorage = battery.Capacity;

                End();
            }

            var bgDrawList = GetBackgroundDrawList();

            foreach (var network in _state.Networks.Values)
            {
                var displayNet = _displayNetworks[network.Id];
                foreach (var supplyId in network.Supplies)
                {
                    var supply = _displaySupplies[supplyId];
                    DrawArrowLine(bgDrawList, displayNet.CurrentWindowPos, supply.CurrentWindowPos, Color.LawnGreen);
                }

                foreach (var loadId in network.Loads)
                {
                    var load = _displayLoads[loadId];
                    DrawArrowLine(bgDrawList, load.CurrentWindowPos, displayNet.CurrentWindowPos, Color.Red);
                }

                foreach (var batteryId in network.BatteryLoads)
                {
                    var battery = _displayBatteries[batteryId];
                    DrawArrowLine(bgDrawList, battery.CurrentWindowPos, displayNet.CurrentWindowPos, Color.Purple);
                }

                foreach (var batteryId in network.BatterySupplies)
                {
                    var battery = _displayBatteries[batteryId];
                    DrawArrowLine(bgDrawList, displayNet.CurrentWindowPos, battery.CurrentWindowPos, Color.Cyan);
                }
            }

            if (_showDemo)
            {
                ShowDemoWindow();
            }

            var reLink = false;
            while (_remQueue.TryDequeue(out var item))
            {
                switch (item)
                {
                    case Network n:
                        _state.Networks.Free(n.Id);
                        _displayNetworks.Remove(n.Id);
                        _state.GroupedNets = null;
                        reLink = true;
                        break;

                    case Supply s:
                        _state.Supplies.Free(s.Id);
                        _state.Networks.Values.ForEach(n => n.Supplies.Remove(s.Id));
                        _displaySupplies.Remove(s.Id);
                        break;

                    case Load l:
                        _state.Loads.Free(l.Id);
                        _state.Networks.Values.ForEach(n => n.Loads.Remove(l.Id));
                        _displayLoads.Remove(l.Id);
                        break;

                    case Battery b:
                        _state.Batteries.Free(b.Id);
                        _state.Networks.Values.ForEach(n => n.BatteryLoads.Remove(b.Id));
                        _state.Networks.Values.ForEach(n => n.BatterySupplies.Remove(b.Id));
                        _displayBatteries.Remove(b.Id);
                        _state.GroupedNets = null;
                        break;
                }
            }

            if (reLink)
                RefreshLinks();
        }


        private void DrawArrowLine(ImDrawListPtr ptr, Vector2 a, Vector2 b, Color color)
        {
            // A: to
            // B: from

            const float wingLength = 15;
            const float thickness = 3;

            var cvtColor = CvtColor(color);

            ptr.AddLine(a, b, cvtColor, thickness);

            var angleA = Angle.FromDegrees(45);
            var angleB = Angle.FromDegrees(-45);

            var mid = (a + b) / 2;
            var dir = -Vector2.Normalize(a - b);

            var rVec = new RobustVec2(dir.X, dir.Y);

            var wingADir = CvtVec(angleA.RotateVec(rVec));
            var wingBDir = CvtVec(angleB.RotateVec(rVec));

            var wingA = wingADir * wingLength + mid;
            var wingB = wingBDir * wingLength + mid;

            ptr.AddLine(mid, wingA, cvtColor, thickness);
            ptr.AddLine(mid, wingB, cvtColor, thickness);
        }

        private static uint CvtColor(Color color)
        {
            return color.R | ((uint) color.G << 8) | ((uint) color.B << 16) | ((uint) color.A << 24);
        }

        private static Vector2 CalcWindowCenter()
        {
            return GetWindowPos() + GetWindowSize() / 2;
        }

        private static Vector2 CvtVec(RobustVec2 vec)
        {
            return new Vector2(vec.X, vec.Y);
        }

        private sealed class DisplayNetwork
        {
            public Vector2 CurrentWindowPos;
        }

        private sealed class DisplayBattery
        {
            public Vector2 CurrentWindowPos;
            public readonly float[] ReceivingPowerData = new float[MaxTickData];
            public readonly float[] SuppliedPowerData = new float[MaxTickData];
            public readonly float[] StoredPowerData = new float[MaxTickData];
        }

        private sealed class DisplayLoad
        {
            public Vector2 CurrentWindowPos;
            public readonly float[] ReceivedPowerData = new float[MaxTickData];
        }

        private sealed class DisplaySupply
        {
            public Vector2 CurrentWindowPos;
            public readonly float[] SuppliedPowerData = new float[MaxTickData];
        }
    }
}

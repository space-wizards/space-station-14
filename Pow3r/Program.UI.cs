using System;
using ImGuiNET;
using Robust.Shared.Maths;
using static ImGuiNET.ImGui;
using Color = System.Drawing.Color;
using Vector2 = System.Numerics.Vector2;
using RobustVec2 = Robust.Shared.Maths.Vector2;
using static Pow3r.PowerState;

namespace Pow3r
{
    internal sealed partial class Program
    {
        private bool _showDemo;

        private void DoDraw(float frameTime)
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
                var id = AllocId();
                _state.Supplies.Add(id, new Supply(id));
            }

            if (Button("Load"))
            {
                var id = AllocId();
                _state.Loads.Add(id, new Load(id));
            }

            if (Button("Network"))
            {
                var id = AllocId();
                _state.Networks.Add(id, new Network(id));
            }

            if (Button("Battery"))
            {
                var id = AllocId();
                _state.Batteries.Add(id, new Battery(id));
            }

            Checkbox("Paused", ref _paused);
            SliderInt("TPS", ref _tps, 1, 120);
            SetNextItemWidth(-1);
            Combo("", ref _currentSolver, _solverNames, _solverNames.Length);

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
                Begin($"Network {network.Id}##Gen{network.Id}");

                Text($"Height: {network.Height}");

                network.CurrentWindowPos = CalcWindowCenter();

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
                Begin($"Load {load.Id}##Load{load.Id}");

                Checkbox("Enabled", ref load.Enabled);
                SliderFloat("Desired", ref load.DesiredPower, 0, 1000, "%.0f W");

                load.CurrentWindowPos = CalcWindowCenter();

                PlotLines("", ref load.ReceivedPowerData[0], MaxTickData, _tickDataIdx + 1,
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
                Begin($"Generator {supply.Id}##Gen{supply.Id}");

                Checkbox("Enabled", ref supply.Enabled);
                SliderFloat("Available", ref supply.MaxSupply, 0, 1000, "%.0f W");
                SliderFloat("Ramp", ref supply.SupplyRampRate, 0, 100, "%.0f W/s");
                SliderFloat("Tolerance", ref supply.SupplyRampTolerance, 0, 100, "%.0f W");

                supply.CurrentWindowPos = CalcWindowCenter();

                Text($"Ramp Position: {supply.SupplyRampPosition:N1}");

                PlotLines("", ref supply.SuppliedPowerData[0], MaxTickData, _tickDataIdx + 1,
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
                Begin($"Battery {battery.Id}##Bat{battery.Id}");

                Checkbox("Enabled", ref battery.Enabled);
                SliderFloat("Capacity", ref battery.Capacity, 0, 100000, "%.0f J");
                SliderFloat("Max charge rate", ref battery.MaxChargeRate, 0, 1000, "%.0f W");
                SliderFloat("Max supply", ref battery.MaxSupply, 0, 1000, "%.0f W");
                SliderFloat("Ramp", ref battery.SupplyRampRate, 0, 100, "%.0f W/s");
                SliderFloat("Tolerance", ref battery.SupplyRampTolerance, 0, 100, "%.0f W");

                battery.CurrentWindowPos = CalcWindowCenter();

                SliderFloat("Ramp position", ref battery.SupplyRampPosition, 0, battery.MaxSupply, "%.0f W");

                PlotLines("", ref battery.SuppliedPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"Supply: {battery.CurrentSupply:N1} W",
                    0, 1000, new Vector2(250, 75));

                PlotLines("", ref battery.ReceivingPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"Load: {battery.CurrentReceiving:N1} W",
                    0, 1000, new Vector2(250, 75));

                PlotLines("", ref battery.StoredPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"Charge: {battery.CurrentStorage:N1} J",
                    0, battery.Capacity, new Vector2(250, 75));

                if (Button("Delete"))
                {
                    _remQueue.Enqueue(battery);
                }

                SameLine();
                if (_linking != null)
                {
                    if (battery.LinkedNetworkLoading == default && Button("Link as load"))
                    {
                        _linking.BatteriesLoading.Add(battery.Id);
                        _linking = null;
                        RefreshLinks();
                    }
                    else
                    {
                        SameLine();
                        if (battery.LinkedNetworkSupplying == default && Button("Link as supply"))
                        {
                            _linking.BatteriesSupplying.Add(battery.Id);
                            _linking = null;
                            RefreshLinks();
                        }
                    }
                }
                else
                {
                    if (battery.LinkedNetworkLoading != default && Button("Unlink loading"))
                    {
                        var net = _state.Networks[battery.LinkedNetworkLoading];
                        net.BatteriesLoading.Remove(battery.Id);
                        battery.LinkedNetworkLoading = default;
                    }
                    else
                    {
                        SameLine();
                        if (battery.LinkedNetworkSupplying != default && Button("Unlink supplying"))
                        {
                            var net = _state.Networks[battery.LinkedNetworkSupplying];
                            net.BatteriesSupplying.Remove(battery.Id);
                            battery.LinkedNetworkSupplying = default;
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
                foreach (var supplyId in network.Supplies)
                {
                    var supply = _state.Supplies[supplyId];
                    DrawArrowLine(bgDrawList, network.CurrentWindowPos, supply.CurrentWindowPos, Color.LawnGreen);
                }

                foreach (var loadId in network.Loads)
                {
                    var load = _state.Loads[loadId];
                    DrawArrowLine(bgDrawList, load.CurrentWindowPos, network.CurrentWindowPos, Color.Red);
                }

                foreach (var batteryId in network.BatteriesLoading)
                {
                    var battery = _state.Batteries[batteryId];
                    DrawArrowLine(bgDrawList, battery.CurrentWindowPos, network.CurrentWindowPos, Color.Purple);
                }

                foreach (var batteryId in network.BatteriesSupplying)
                {
                    var battery = _state.Batteries[batteryId];
                    DrawArrowLine(bgDrawList, network.CurrentWindowPos, battery.CurrentWindowPos, Color.Cyan);
                }
            }

            if (_showDemo)
            {
                ShowDemoWindow();
            }

            while (_remQueue.TryDequeue(out var item))
            {
                switch (item)
                {
                    case Network n:
                        _state.Networks.Remove(n.Id);
                        break;

                    case Supply s:
                        _state.Supplies.Remove(s.Id);
                        _state.Networks.Values.ForEach(n => n.Supplies.Remove(s.Id));
                        break;

                    case Load l:
                        _state.Loads.Remove(l.Id);
                        _state.Networks.Values.ForEach(n => n.Loads.Remove(l.Id));
                        break;

                    case Battery b:
                        _state.Batteries.Remove(b.Id);
                        _state.Networks.Values.ForEach(n => n.BatteriesLoading.Remove(b.Id));
                        _state.Networks.Values.ForEach(n => n.BatteriesSupplying.Remove(b.Id));
                        break;
                }
            }
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
    }
}

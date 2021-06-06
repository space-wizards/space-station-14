using ImGuiNET;
using Robust.Shared.Maths;
using static ImGuiNET.ImGui;
using Color = System.Drawing.Color;
using Vector2 = System.Numerics.Vector2;
using RobustVec2 = Robust.Shared.Maths.Vector2;

namespace Pow3r
{
    internal sealed partial class Program
    {
        private void DoDraw(float frameTime)
        {
            if (BeginMainMenuBar())
            {
                _showDemo ^= MenuItem("Demo");
                EndMainMenuBar();
            }

            SetNextWindowSize(new Vector2(100, 150));

            Begin("CreateButtons",
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoResize);

            if (Button("Generator"))
            {
                var id = AllocId();
                _supplies.Add(id, new Supply(id));
            }

            if (Button("Load"))
            {
                var id = AllocId();
                _loads.Add(id, new Load(id));
            }

            if (Button("Network"))
            {
                var id = AllocId();
                _networks.Add(id, new Network(id));
            }

            if (Button("Battery"))
            {
                var id = AllocId();
                _batteries.Add(id, new Battery(id));
            }

            Checkbox("Paused", ref _paused);

            End();

            Begin("Timing");

            PlotLines("Tick time (ms)", ref _simTickTimes[0], MaxTickData, _tickDataIdx + 1,
                $"{_simTickTimes[_tickDataIdx]:N2}",
                0,
                0.1f, new Vector2(250, 150));

            End();

            foreach (var network in _networks.Values)
            {
                Begin($"Network##Gen{network.Id}");

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

            foreach (var load in _loads.Values)
            {
                Begin($"Load##Load{load.Id}");

                Checkbox("Enabled", ref load.Enabled);
                SliderFloat("Desired", ref load.DesiredPower, 0, 1000, "%.0f W");

                load.CurrentWindowPos = CalcWindowCenter();

                PlotLines("", ref load.ReceivedPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"Receiving: {load.ReceivingPower:N1} W",
                    0,
                    1000, new Vector2(250, 150));

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
                        var net = _networks[load.LinkedNetwork];
                        net.Loads.Remove(load.Id);
                        load.LinkedNetwork = default;
                    }
                }

                End();
            }

            foreach (var supply in _supplies.Values)
            {
                Begin($"Generator##Gen{supply.Id}");

                Checkbox("Enabled", ref supply.Enabled);
                SliderFloat("Available", ref supply.MaxSupply, 0, 1000, "%.0f W");
                SliderFloat("Ramp", ref supply.SupplyRampRate, 0, 100, "%.0f W");
                SliderFloat("Tolerance", ref supply.SupplyRampTolerance, 0, 100, "%.0f W");

                supply.CurrentWindowPos = CalcWindowCenter();

                Text($"Ramp Position: {supply.SupplyRampPosition:N1}");

                PlotLines("", ref supply.SuppliedPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"Supply: {supply.CurrentSupply:N1} W",
                    0, 1000, new Vector2(250, 150));

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
                        var net = _networks[supply.LinkedNetwork];
                        net.Supplies.Remove(supply.Id);
                        supply.LinkedNetwork = default;
                    }
                }

                End();
            }

            foreach (var battery in _batteries.Values)
            {
                Begin($"Battery##Bat{battery.Id}");

                Checkbox("Enabled", ref battery.Enabled);
                SliderFloat("Capacity", ref battery.Capacity, 0, 100000, "%.0f J");
                SliderFloat("Max supply", ref battery.MaxSupply, 0, 1000, "%.0f W");
                SliderFloat("Ramp", ref battery.SupplyRampRate, 0, 100, "%.0f W");
                SliderFloat("Tolerance", ref battery.SupplyRampTolerance, 0, 100, "%.0f W");

                battery.CurrentWindowPos = CalcWindowCenter();

                Text($"Ramp Position: {battery.SupplyRampPosition:N1}");

                PlotLines("", ref battery.SuppliedPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"Supply: {battery.CurrentSupply:N1} W",
                    0, 1000, new Vector2(250, 75));

                PlotLines("", ref battery.StoredPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"Charge: {battery.CurrentStorage:N1} W",
                    0, 100000, new Vector2(250, 75));

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
                        var net = _networks[battery.LinkedNetworkLoading];
                        net.BatteriesLoading.Remove(battery.Id);
                        battery.LinkedNetworkLoading = default;
                    }
                    else
                    {
                        SameLine();
                        if (battery.LinkedNetworkSupplying != default && Button("Unlink supplying"))
                        {
                            var net = _networks[battery.LinkedNetworkSupplying];
                            net.BatteriesSupplying.Remove(battery.Id);
                            battery.LinkedNetworkSupplying = default;
                        }
                    }
                }

                End();
            }

            var bgDrawList = GetBackgroundDrawList();

            foreach (var network in _networks.Values)
            {
                foreach (var supplyId in network.Supplies)
                {
                    var supply = _supplies[supplyId];
                    DrawArrowLine(bgDrawList, network.CurrentWindowPos, supply.CurrentWindowPos, Color.LawnGreen);
                }

                foreach (var loadId in network.Loads)
                {
                    var load = _loads[loadId];
                    DrawArrowLine(bgDrawList, load.CurrentWindowPos, network.CurrentWindowPos, Color.Red);
                }

                foreach (var batteryId in network.BatteriesLoading)
                {
                    var battery = _batteries[batteryId];
                    DrawArrowLine(bgDrawList, battery.CurrentWindowPos, network.CurrentWindowPos, Color.Purple);
                }

                foreach (var batteryId in network.BatteriesSupplying)
                {
                    var battery = _batteries[batteryId];
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
                        _networks.Remove(n.Id);
                        break;

                    case Supply s:
                        _supplies.Remove(s.Id);
                        _networks.Values.ForEach(n => n.Supplies.Remove(s.Id));
                        break;

                    case Load l:
                        _loads.Remove(l.Id);
                        _networks.Values.ForEach(n => n.Loads.Remove(l.Id));
                        break;

                    case Battery b:
                        _batteries.Remove(b.Id);
                        _networks.Values.ForEach(n => n.BatteriesLoading.Remove(b.Id));
                        _networks.Values.ForEach(n => n.BatteriesSupplying.Remove(b.Id));
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

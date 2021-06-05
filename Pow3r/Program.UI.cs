using System.Drawing;
using System.Numerics;
using ImGuiNET;
using static ImGuiNET.ImGui;

namespace Pow3r
{
    internal sealed unsafe partial class Program
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
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize);

            if (Button("Generator"))
            {
                _supplies.Add(new Supply(_nextId++));
            }

            if (Button("Load"))
            {
                _loads.Add(new Load(_nextId++));
            }

            if (Button("Network"))
            {
                _networks.Add(new Network(_nextId++));
            }

            if (Button("Battery"))
            {
                _batteries.Add(new Battery(_nextId++));
            }

            Checkbox("Paused", ref _paused);

            End();

            Begin("Timing");

            PlotLines("Tick time (ms)", ref _simTickTimes[0], MaxTickData, _tickDataIdx + 1,
                $"",
                0,
                0.1f, new Vector2(250, 150));

            End();

            foreach (var network in _networks)
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

            foreach (var load in _loads)
            {
                Begin($"Load##Load{load.Id}");

                Checkbox("Enabled", ref load.Enabled);
                SliderFloat("Desired", ref load.DesiredPower, 0, 1000, "%.0f W");

                load.CurrentWindowPos = CalcWindowCenter();

                PlotLines("", ref load.ReceivedPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"{load.ReceivingPower:N1} W",
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
                        _linking.Loads.Add(load);
                        _linking = null;
                        RefreshLinks();
                    }
                }
                else
                {
                    if (load.LinkedNetwork != null && Button("Unlink"))
                    {
                        load.LinkedNetwork.Loads.Remove(load);
                        load.LinkedNetwork = null;
                    }
                }

                End();
            }

            foreach (var supply in _supplies)
            {
                Begin($"Generator##Gen{supply.Id}");

                Checkbox("Enabled", ref supply.Enabled);
                SliderFloat("Available", ref supply.MaxSupply, 0, 1000, "%.0f W");
                SliderFloat("Ramp", ref supply.SupplyRampRate, 0, 100, "%.0f W");
                SliderFloat("Tolerance", ref supply.SupplyRampTolerance, 0, 100, "%.0f W");

                supply.CurrentWindowPos = CalcWindowCenter();

                Text($"Ramp Position: {supply.SupplyRampPosition:N1}");

                PlotLines("", ref supply.SuppliedPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"{supply.CurrentSupply:N1} W",
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
                        _linking.Supplies.Add(supply);
                        _linking = null;
                        RefreshLinks();
                    }
                }
                else
                {
                    if (supply.LinkedNetwork != null && Button("Unlink"))
                    {
                        supply.LinkedNetwork.Supplies.Remove(supply);
                        supply.LinkedNetwork = null;
                    }
                }

                End();
            }

            foreach (var battery in _batteries)
            {
                Begin($"Battery##Bat{battery.Id}");

                Checkbox("Enabled", ref battery.Enabled);
                SliderFloat("Available", ref battery.MaxSupply, 0, 1000, "%.0f W");
                SliderFloat("Ramp", ref battery.SupplyRampRate, 0, 100, "%.0f W");
                SliderFloat("Tolerance", ref battery.SupplyRampTolerance, 0, 100, "%.0f W");

                battery.CurrentWindowPos = CalcWindowCenter();

                Text($"Ramp Position: {battery.SupplyRampPosition:N1}");

                PlotLines("", ref battery.SuppliedPowerData[0], MaxTickData, _tickDataIdx + 1,
                    $"{battery.CurrentSupply:N1} W",
                    0, 1000, new Vector2(250, 150));

                if (Button("Delete"))
                {
                    _remQueue.Enqueue(battery);
                }

                SameLine();
                if (_linking != null)
                {
                    if (battery.LinkedNetworkLoading == null && Button("Link as load"))
                    {
                        _linking.BatteriesLoading.Add(battery);
                        _linking = null;
                        RefreshLinks();
                    }
                    else
                    {
                        SameLine();
                        if (battery.LinkedNetworkSupplying == null && Button("Link as supply"))
                        {
                            _linking.BatteriesSupplying.Add(battery);
                            _linking = null;
                            RefreshLinks();
                        }
                    }
                }
                else
                {
                    if (battery.LinkedNetworkLoading != null && Button("Unlink loading"))
                    {
                        battery.LinkedNetworkLoading.BatteriesLoading.Remove(battery);
                        battery.LinkedNetworkLoading = null;
                    }
                    else
                    {
                        SameLine();
                        if (battery.LinkedNetworkSupplying != null && Button("Unlink supplying"))
                        {
                            battery.LinkedNetworkSupplying.BatteriesSupplying.Remove(battery);
                            battery.LinkedNetworkSupplying = null;
                        }
                    }
                }

                End();
            }

            var bgDrawList = GetBackgroundDrawList();

            foreach (var network in _networks)
            {
                foreach (var generator in network.Supplies)
                {
                    bgDrawList.AddLine(network.CurrentWindowPos, generator.CurrentWindowPos, CvtColor(Color.LawnGreen),
                        3);
                }

                foreach (var load in network.Loads)
                {
                    bgDrawList.AddLine(network.CurrentWindowPos, load.CurrentWindowPos, CvtColor(Color.Red), 3);
                }

                foreach (var battery in network.BatteriesLoading)
                {
                    bgDrawList.AddLine(network.CurrentWindowPos, battery.CurrentWindowPos, CvtColor(Color.Purple), 3);
                }

                foreach (var battery in network.BatteriesSupplying)
                {
                    bgDrawList.AddLine(network.CurrentWindowPos, battery.CurrentWindowPos, CvtColor(Color.Cyan), 3);
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
                        _networks.Remove(n);
                        break;

                    case Supply s:
                        _supplies.Remove(s);
                        _networks.ForEach(n => n.Supplies.Remove(s));
                        break;

                    case Load l:
                        _loads.Remove(l);
                        _networks.ForEach(n => n.Loads.Remove(l));
                        break;

                    case Battery b:
                        _batteries.Remove(b);
                        _networks.ForEach(n => n.BatteriesLoading.Remove(b));
                        _networks.ForEach(n => n.BatteriesSupplying.Remove(b));
                        break;
                }
            }
        }


        private static uint CvtColor(Color color)
        {
            return color.R | ((uint) color.G << 8) | ((uint) color.B << 16) | ((uint) color.A << 24);
        }

        private static Vector2 CalcWindowCenter()
        {
            return GetWindowPos() + GetWindowSize() / 2;
        }
    }
}

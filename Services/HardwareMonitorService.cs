using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Logging;

namespace OLED_Customizer.Services
{
    public class HardwareMonitorService : IDisposable
    {
        private readonly ILogger<HardwareMonitorService> _logger;
        private readonly Computer _computer;
        private readonly UpdateVisitor _updateVisitor;
        
        // Cache
        private (float? cpuTemp, float? cpuLoad, float? gpuTemp, float? gpuLoad, float? ramUsed, float? ramAvailable) _cachedStats;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public HardwareMonitorService(ILogger<HardwareMonitorService> logger)
        {
            _logger = logger;
            _updateVisitor = new UpdateVisitor();
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsControllerEnabled = true, 
                IsNetworkEnabled = false,
                IsStorageEnabled = false
            };

            try
            {
                _computer.Open();
                // Start Update Loop
                Task.Run(() => UpdateLoop(_cts.Token));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open Hardware Monitor");
            }
        }

        private async Task UpdateLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    UpdateStats();
                }
                catch { }
                await Task.Delay(1000, token); // Update every 1s
            }
        }

        private void UpdateStats()
        {
            try
            {
                _computer.Accept(_updateVisitor);

                float? cpuTemp = null, cpuLoad = null;
                float? gpuTemp = null, gpuLoad = null;
                float? ramUsed = null, ramAvailable = null;

                foreach (var hardware in _computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.Cpu)
                    {
                        hardware.Update(); 
                        var load = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "CPU Total");
                        if (load != null) cpuLoad = load.Value;

                        var temp = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Tctl"))
                                   ?? hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Package")) 
                                   ?? hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                        if (temp != null) cpuTemp = temp.Value;
                    }
                    else if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuIntel)
                    {
                        hardware.Update();
                        var load = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "GPU Core");
                        if (load != null) gpuLoad = load.Value;
                        
                        var temp = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name == "GPU Core");
                        if (temp != null) gpuTemp = temp.Value;
                    }
                    else if (hardware.HardwareType == HardwareType.Memory)
                    {
                         hardware.Update();
                         var used = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Used");
                         var avail = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Available");
                         
                         if (used != null) ramUsed = used.Value;
                         if (avail != null) ramAvailable = avail.Value;
                    }
                }

                _cachedStats = (cpuTemp, cpuLoad, gpuTemp, gpuLoad, ramUsed, ramAvailable);
            }
            catch (Exception)
            {
                // Ignore transient errors
            }
        }

        public (float? cpuTemp, float? cpuLoad, float? gpuTemp, float? gpuLoad, float? ramUsed, float? ramAvailable) GetStats()
        {
            return _cachedStats;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _computer.Close();
        }

        private class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (var subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }
    }
}

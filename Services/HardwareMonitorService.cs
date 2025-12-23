using System;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Logging;

namespace OLED_Customizer.Services
{
    public class HardwareMonitorService : IDisposable
    {
        private readonly ILogger<HardwareMonitorService> _logger;
        private readonly Computer _computer;
        private readonly UpdateVisitor _updateVisitor;

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
                IsNetworkEnabled = true,
                IsStorageEnabled = true
            };

            try
            {
                _computer.Open();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open Hardware Monitor (Admin rights might be required)");
            }
        }

        public (float? cpuTemp, float? cpuLoad, float? gpuTemp, float? gpuLoad, float? ramLoad) GetStats()
        {
            try
            {
                _computer.Accept(_updateVisitor);

                float? cpuTemp = null;
                float? cpuLoad = null;
                float? gpuTemp = null;
                float? gpuLoad = null;
                float? ramLoad = null;

                foreach (var hardware in _computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.Cpu)
                    {
                        hardware.Update(); // Force update? Visitor should handle it.
                        // CPU Load
                        var load = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "CPU Total");
                        if (load != null) cpuLoad = load.Value;

                        // CPU Temp (Package or average)
                        var temp = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Package")) 
                                   ?? hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                        if (temp != null) cpuTemp = temp.Value;
                    }
                    else if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd)
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
                         var load = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "Memory");
                         if (load != null) ramLoad = load.Value;
                    }
                }

                return (cpuTemp, cpuLoad, gpuTemp, gpuLoad, ramLoad);
            }
            catch (Exception ex)
            {
               // _logger.LogWarning($"HW Monitor Error: {ex.Message}");
                return (null, null, null, null, null);
            }
        }

        public void Dispose()
        {
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

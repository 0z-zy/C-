using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OLED_Customizer.Core;
using OLED_Customizer.Services;
using OLED_Customizer.UI;

namespace OLED_Customizer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var services = new ServiceCollection();
            ConfigureServices(services);

            using (var serviceProvider = services.BuildServiceProvider())
            {
                var trayContext = serviceProvider.GetRequiredService<TrayContext>();
                Application.Run(trayContext);
            }
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            services.AddLogging(configure => 
            {
                configure.AddConsole();
                configure.AddDebug();
            });

            services.AddSingleton<AppConfig>(provider => AppConfig.Load("config.json"));
            
            services.AddSingleton<SteelSeriesAPI>();
            services.AddSingleton<HardwareMonitorService>();
            services.AddSingleton<MediaService>();
            
            services.AddSingleton<DisplayManager>();
            services.AddSingleton<TrayContext>();
        }
    }
}
using System;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using OLED_Customizer.Core;

namespace OLED_Customizer.UI
{
    public class TrayContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly DisplayManager _displayManager;
        private readonly AppConfig _config;
        private readonly ILogger<TrayContext> _logger;

        public TrayContext(DisplayManager displayManager, AppConfig config, ILogger<TrayContext> logger)
        {
            _displayManager = displayManager;
            _config = config;
            _logger = logger;

            var exitMenuItem = new ToolStripMenuItem("Exit", null, Exit);
            var settingsMenuItem = new ToolStripMenuItem("Settings", null, ShowSettings);

            _notifyIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon("icon.ico"),
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = "OLED Customizer"
            };

            _notifyIcon.ContextMenuStrip.Items.Add(settingsMenuItem);
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            _notifyIcon.ContextMenuStrip.Items.Add(exitMenuItem);

            _displayManager.Start();
        }

        private SettingsForm? _settingsForm;

        private void ShowSettings(object? sender, EventArgs e)
        {
            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsForm(_config);
                _settingsForm.Show();
            }
            else
            {
                _settingsForm.BringToFront();
            }
        }

        private void Exit(object? sender, EventArgs e)
        {
            _displayManager.Stop();
            _notifyIcon.Visible = false;
            Application.Exit();
        }
    }
}

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
        private readonly ILogger<TrayContext> _logger;

        public TrayContext(DisplayManager displayManager, ILogger<TrayContext> logger)
        {
            _displayManager = displayManager;
            _logger = logger;

            var exitMenuItem = new ToolStripMenuItem("Exit", null, Exit);
            var settingsMenuItem = new ToolStripMenuItem("Settings (TODO)", null, ShowSettings);

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application, // Placeholder icon
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = "OLED Customizer"
            };

            _notifyIcon.ContextMenuStrip.Items.Add(settingsMenuItem);
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            _notifyIcon.ContextMenuStrip.Items.Add(exitMenuItem);

            _displayManager.Start();
        }

        private void ShowSettings(object? sender, EventArgs e)
        {
            // TODO: Settings Form
            MessageBox.Show("Settings not implemented yet.");
        }

        private void Exit(object? sender, EventArgs e)
        {
            _displayManager.Stop();
            _notifyIcon.Visible = false;
            Application.Exit();
        }
    }
}

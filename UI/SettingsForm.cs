using System;
using System.Drawing;
using System.Windows.Forms;
using OLED_Customizer.Core;

namespace OLED_Customizer.UI
{
    public partial class SettingsForm : Form
    {
        private readonly AppConfig _config;
        private TabControl _tabControl;

        public SettingsForm(AppConfig config)
        {
            _config = config;
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "OLED Customizer - Settings";
            this.Size = new Size(600, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Icon = new Icon("icon.ico");

            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(10, 5)
            };

            // Tabs
            _tabControl.TabPages.Add(CreateGeneralTab());
            _tabControl.TabPages.Add(CreateDisplayTab());
            _tabControl.TabPages.Add(CreateSpotifyTab());
            _tabControl.TabPages.Add(CreateLightingTab());
            _tabControl.TabPages.Add(CreateAdvancedTab());

            this.Controls.Add(_tabControl);
            
            // Save Button
            var saveBtn = new Button
            {
                Text = "SAVE",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            saveBtn.Click += (s, e) => SaveSettings();
            this.Controls.Add(saveBtn);
        }

        // --- Tab Creation Helpers ---

        private TabPage CreateGeneralTab()
        {
            var tab = new TabPage("General");
            var layout = CreateLayout(tab);

            AddCombo(layout, "Clock Style:", 
                new[] { "Standard", "Big Timer", "Date Focused", "Analog" }, 
                _config.ClockStyle, v => _config.ClockStyle = v);
            
            AddCheck(layout, "Show Seconds", _config.DisplaySeconds, v => _config.DisplaySeconds = v);
            AddCheck(layout, "Use Turkish Language", _config.UseTurkishDays, v => _config.UseTurkishDays = v);

            return tab;
        }

        private TabPage CreateDisplayTab()
        {
            var tab = new TabPage("Display");
            var layout = CreateLayout(tab);

            AddCheck(layout, "Enable Clock", _config.DisplayClock, v => _config.DisplayClock = v);
            AddCheck(layout, "Enable Music Info", _config.DisplayPlayer, v => _config.DisplayPlayer = v);
            AddCheck(layout, "Show Hardware Stats", _config.DisplayHwMonitor, v => _config.DisplayHwMonitor = v);

            return tab;
        }

        private TabPage CreateSpotifyTab()
        {
            var tab = new TabPage("Spotify");
            var layout = CreateLayout(tab);

            AddLabel(layout, "Spotify Credentials (Required for API):");
            AddText(layout, "Client ID:", _config.SpotifyClientId, v => _config.SpotifyClientId = v);
            AddText(layout, "Client Secret:", _config.SpotifyClientSecret, v => _config.SpotifyClientSecret = v);
            AddText(layout, "Redirect URI:", _config.SpotifyRedirectUri, v => _config.SpotifyRedirectUri = v);
            AddText(layout, "Port:", _config.LocalPort.ToString(), v => { if (int.TryParse(v, out int p)) _config.LocalPort = p; });

            return tab;
        }

        private TabPage CreateLightingTab()
        {
            var tab = new TabPage("Lighting");
            var layout = CreateLayout(tab);

            AddCheck(layout, "RGB Enabled", _config.RgbEnabled, v => _config.RgbEnabled = v);
            
            // RGB Color Picker
            var colorBtn = new Button { Text = "Pick Color", Width = 100, AutoSize = true };
            var colorPanel = new Panel { Width = 30, Height = 30, BackColor = Color.FromArgb(_config.RgbColor[0], _config.RgbColor[1], _config.RgbColor[2]) };
            
            colorBtn.Click += (s, e) => 
            {
                using var cd = new ColorDialog { Color = colorPanel.BackColor };
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    colorPanel.BackColor = cd.Color;
                    _config.RgbColor = new int[] { cd.Color.R, cd.Color.G, cd.Color.B };
                }
            };

            var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            flow.Controls.Add(colorBtn);
            flow.Controls.Add(colorPanel);
            layout.Controls.Add(flow);

            return tab;
        }

        private TabPage CreateAdvancedTab()
        {
            var tab = new TabPage("Advanced");
            var layout = CreateLayout(tab);

            AddText(layout, "Scrollbar Padding:", _config.ScrollbarPadding.ToString(), v => { if(int.TryParse(v, out int i)) _config.ScrollbarPadding = i; });
            AddText(layout, "Text Padding:", _config.TextPaddingLeft.ToString(), v => { if(int.TryParse(v, out int i)) _config.TextPaddingLeft = i; });
            AddText(layout, "FPS:", _config.Fps.ToString(), v => { if(int.TryParse(v, out int i)) _config.Fps = i; });

            return tab;
        }

        // --- UI Helpers ---

        private FlowLayoutPanel CreateLayout(TabPage tab)
        {
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(10),
                AutoScroll = true
            };
            tab.Controls.Add(flow);
            return flow;
        }

        private void AddCheck(FlowLayoutPanel p, string text, bool val, Action<bool> onChange)
        {
            var chk = new CheckBox { Text = text, Checked = val, AutoSize = true, Padding = new Padding(0, 5, 0, 5) };
            chk.CheckedChanged += (s, e) => onChange(chk.Checked);
            p.Controls.Add(chk);
        }

        private void AddText(FlowLayoutPanel p, string label, string val, Action<string> onChange)
        {
            AddLabel(p, label);
            var txt = new TextBox { Text = val, Width = 250 };
            txt.TextChanged += (s, e) => onChange(txt.Text);
            p.Controls.Add(txt);
        }

        private void AddCombo(FlowLayoutPanel p, string label, string[] options, string val, Action<string> onChange)
        {
            AddLabel(p, label);
            var cb = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cb.Items.AddRange(options);
            cb.SelectedItem = val;
            cb.SelectedIndexChanged += (s, e) => onChange(cb.SelectedItem?.ToString() ?? options[0]);
            p.Controls.Add(cb);
        }

        private void AddLabel(FlowLayoutPanel p, string text)
        {
            p.Controls.Add(new Label { Text = text, AutoSize = true, Padding = new Padding(0, 10, 0, 0) });
        }

        private void LoadSettings() { /* Handled in construction via closures */ }

        private void SaveSettings()
        {
            _config.SavePreferences();
            this.Close();
        }
    }
}

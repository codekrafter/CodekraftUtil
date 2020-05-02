using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CodekraftUtil
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            RefreshPorts();

            portsRefreshButton.Click += (s, args) => RefreshPorts();
            portsComboBox.SelectedItem = Properties.Settings.Default["DefaultPort"];
            AppContext.Current.SetComPort((string)portsComboBox.SelectedItem);

            portsComboBox.SelectedIndexChanged += (s, args) =>
            {
                AppContext.Current.SetComPort(portsComboBox.SelectedItem.ToString());
                Properties.Settings.Default["DefaultPort"] = portsComboBox.SelectedItem.ToString();
                Properties.Settings.Default.Save();
            };

            AppContext.Current.SetSelectedPortHandler += (port) => portsComboBox.SelectedItem = port;
            AppContext.Current.SetStatusHandler += (status, color) => { statusLabel.Invoke(new Action(() => { statusLabel.Text = status; statusLabel.ForeColor = color; })); };

            RegistryKey runKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (runKey.GetValue("CodekraftUtil") != null)
            {
                if ((string)runKey.GetValue("CodekraftUtil") != Application.ExecutablePath)
                {
                    startupCheckbox.Checked = false;
                    startupCheckbox.CheckState = CheckState.Indeterminate;
                }
                else
                {
                    startupCheckbox.CheckState = CheckState.Checked;
                }
            }
        }

        private void RefreshPorts()
        {
            object current = portsComboBox.SelectedItem;

            portsComboBox.Items.Clear();
            portsComboBox.Items.AddRange(SerialPort.GetPortNames().Prepend("").ToArray());

            if (current == null)
            {
                AppContext.Current.SetComPort(null);
            }
            else
            {
                if (!portsComboBox.Items.Contains(current))
                {
                    AppContext.Current.SetComPort(null);
                }
                else
                {
                    portsComboBox.SelectedItem = current;
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
        }

        private void startupCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            RegistryKey runKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (startupCheckbox.Checked)
            {
                runKey.SetValue("CodekraftUtil", Application.ExecutablePath);
            }
            else
            {
                runKey.DeleteValue("CodekraftUtil", false);
            }
        }
    }
}

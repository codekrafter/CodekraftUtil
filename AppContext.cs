using CodekraftUtil.Properties;
//using DiscordRPC;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO.Ports;

namespace CodekraftUtil
{
    public class AppContext : ApplicationContext
    {
        private NotifyIcon trayIcon;

        ContextMenuStrip contextMenu;

        HashSet<IntPtr> lastHandleStack = new HashSet<IntPtr>();

        WindowsHelper.WinEventProcDelegate winEventDelegate;

        public static AppContext Current;

        private SerialPort currentPort = null;

        //DiscordRpcClient Client;
        //RichPresence presence;

        bool DndEnabled = false;

        public AppContext()
        {
            Current = this;

            // Add Num Pad Keys
            FieldInfo info = typeof(SendKeys).GetField("keywords",
    BindingFlags.Static | BindingFlags.NonPublic);
            Array oldKeys = (Array)info.GetValue(null);
            Type elementType = oldKeys.GetType().GetElementType();
            Array newKeys = Array.CreateInstance(elementType, oldKeys.Length + 10);
            Array.Copy(oldKeys, newKeys, oldKeys.Length);
            for (int i = 0; i < 10; i++)
            {
                var newItem = Activator.CreateInstance(elementType, "NUM" + i, (int)Keys.NumPad0 + i);
                newKeys.SetValue(newItem, oldKeys.Length + i);
            }
            info.SetValue(null, newKeys);

            contextMenu = new ContextMenuStrip();
            contextMenu.Items.AddRange(new ToolStripMenuItem[] {
                    new ToolStripMenuItem("Settings", null, (object sender, EventArgs e) => { SettingsForm form = new SettingsForm(); form.Show(); }),
                    new ToolStripMenuItem("Play/Pause Spotify", Resources.SpotifyImage, (object sender, EventArgs e) => SendKeys.Send("{NUM0}")),
                    new ToolStripMenuItem("Toggle Discord DnD", Resources.DiscordImage, ToggleDiscordDnD),
                    new ToolStripMenuItem("Exit", null, Exit)
                });
            trayIcon = new NotifyIcon()
            {
                Icon = Resources.CodekraftIcon,
                ContextMenuStrip = contextMenu,
                Visible = true
            };

            //trayIcon.Click += (object sender, EventArgs e) => contextMenu.Show(Control.MousePosition);

            winEventDelegate = WinEventProc;
            WindowsHelper.SetWinEventHook(3, 3, IntPtr.Zero, winEventDelegate, 0, 0, 0);
        }

        void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        void ToggleDiscordDnD(object sender, EventArgs e)
        {
            WindowsHelper.POINT startCursorPos = WindowsHelper.GetCursorPos();

            Process[] discordProcesses = Process.GetProcesses().Where((p) => p.MainWindowTitle.Contains("Discord")).ToArray();

            if (discordProcesses.Length > 1)
            {
                Console.WriteLine("More than one discord process, going with first one: " + discordProcesses[0].MainWindowTitle);
            }

            Process discord = discordProcesses[0];

            WindowsHelper.WINDOWPLACEMENT placement = WindowsHelper.GetWindowPlacement(discord.MainWindowHandle);

            bool focused = discord.Id == WindowsHelper.GetWindowThreadProcessId((lastHandleStack.Count() > 0) ? lastHandleStack.Last() : IntPtr.Zero);
            bool minimized = placement.showCmd == 2;

            if (!focused || !minimized)
            {
                Console.WriteLine("Showing window...");
                WindowsHelper.ShowWindowAsync(discord.MainWindowHandle, 9);
                WindowsHelper.SetForegroundWindow(discord.MainWindowHandle);
            }

            WindowsHelper.RECT discordRect = WindowsHelper.GetWindowRect(discord.MainWindowHandle);

            WindowsHelper.LeftMouseClick(discordRect.Left + 100, discordRect.Bottom - 20);

            Task t = Task.Run(async () =>
            {
                await Task.Delay(150);
                if (DndEnabled)
                {
                    WindowsHelper.LeftMouseClick(discordRect.Left + 100, discordRect.Bottom - 300);
                }
                else
                {
                    WindowsHelper.LeftMouseClick(discordRect.Left + 100, discordRect.Bottom - 200);
                }
            });

            t.Wait();

            if (minimized)
            {
                WindowsHelper.ShowWindowAsync(discord.MainWindowHandle, 6);
            }

            if (!focused)
            {
                foreach (IntPtr hwnd in lastHandleStack)
                {
                    WindowsHelper.SetForegroundWindow(hwnd);
                }
            }

            DndEnabled = !DndEnabled;

            WindowsHelper.SetCursorPos((int) startCursorPos.x,(int) startCursorPos.y);
        }

        private void WinEventProc(IntPtr hHook, int ev, IntPtr hwnd, int objectId, int childId, int eventThread, int eventTime)
        {
            //Console.WriteLine(Process.GetProcessById(WindowsHelper.GetWindowThreadProcessId(hwnd)).ProcessName);
            //Console.WriteLine(hwnd);
            //Console.WriteLine(WindowsHelper.GetClassName(hwnd));
            //Console.WriteLine(WindowsHelper.FindWindow("NotifyIconOverflowWindow", null) == hwnd);

            if (Process.GetProcessById(WindowsHelper.GetWindowThreadProcessId(hwnd)).ProcessName != "CodekraftUtil" &&
                WindowsHelper.FindWindow("NotifyIconOverflowWindow", null) != hwnd &&
                WindowsHelper.FindWindow("Shell_TrayWnd", null) != hwnd &&
                !Process.GetProcessById(WindowsHelper.GetWindowThreadProcessId(hwnd)).ProcessName.Contains("Discord"))
            {
                //Console.WriteLine(WindowsHelper.GetClassName(hwnd));
                if (lastHandleStack.Contains(hwnd))
                {
                    lastHandleStack.Remove(hwnd);
                }
                lastHandleStack.Add(hwnd);
            }

        }

        public void SetComPort(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (currentPort != null)
                {
                    if (currentPort.IsOpen)
                    {
                        currentPort.Write("OUT SHUTDOWN");
                        currentPort.Close();
                    }
                    currentPort = null;
                    SetStatusHandler.Invoke("Disconnected", Color.Red);
                }
            }
            else
            {
                if (currentPort == null || currentPort.PortName != name)
                {
                    currentPort = new SerialPort(name, 9600);
                    currentPort.Handshake = Handshake.None;
                    try
                    {
                        currentPort.Open();
                        currentPort.DataReceived += DataRecieved;
                        currentPort.Write("OUT INIT");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error opening/initializing port: " + ex.Message, "Error!");
                    }
                }
            }
        }

        private void DataRecieved(object sender, SerialDataReceivedEventArgs e)
        {
            if (currentPort != null)
            {
                string line = currentPort.ReadLine().TrimEnd('\r', '\n');
                Console.WriteLine("|" + line + "|");
                if (line == "INIT")
                {
                    currentPort.Write("OUT INIT");
                }
                else if (line == "ADK OUT INIT")
                {
                    SetStatusHandler.Invoke("Connected", Color.Green);
                }
                else if (line == "SHUTDOWN")
                {
                    SetSelectedPortHandler.Invoke("");
                    SetStatusHandler.Invoke("Disconnected", Color.Red);
                }
                else if (line == "TOGGLE DISCORD")
                {
                    ToggleDiscordDnD(this, new EventArgs());
                }
                else if (line == "TOGGLE MUSIC")
                {
                    SendKeys.Send("{NUM0}");
                }
            }
        }

        public delegate void SetSelectedPortDelegate(string selectedPort);
        public SetSelectedPortDelegate SetSelectedPortHandler;

        public delegate void SetStatusDelegate(string status, Color color);
        public SetStatusDelegate SetStatusHandler;
    }
}

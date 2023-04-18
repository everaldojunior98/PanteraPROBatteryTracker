using System;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using HidLibrary;
using PanteraPRO.Properties;
using Timer = System.Timers.Timer;

namespace PanteraPRO
{
    public class AppContext : ApplicationContext
    {
        #region Fields

        private const int UpdateInterval = 60 * 1000;

        private const float BatteryDecreaseRate = 0.05f;
        private const float BatteryIncreaseRate = 1.1f;

        private const int VendorId = 0x25A7;
        private const int FindDeviceSleepTime = 1000;

        private const string MouseName = "2.4G Dual Mode Mouse";

        private readonly NotifyIcon notifyIcon;
        private readonly Timer timer;

        private readonly ManualResetEvent deviceResetEvent;
        private readonly CancellationTokenSource driverToken;
        private readonly Thread driverThread;

        private float currentBattery;
        private bool isCharging;

        private Icon battery100Icon;
        private Icon battery75Icon;
        private Icon battery50Icon;
        private Icon battery25Icon;
        private Icon batteryWarningIcon;
        private Icon batteryChargingIcon;

        #endregion

        #region Constructor

        public AppContext()
        {
            LoadIcons();

            currentBattery = LoadBatteryPercentage();
            var exitMenuItem = new MenuItem(Resources.Exit, Exit);

            notifyIcon = new NotifyIcon();
            notifyIcon.ContextMenu = new ContextMenu(new[] { exitMenuItem });
            notifyIcon.Visible = true;

            UpdateVisuals();

            timer = new Timer(UpdateInterval);
            timer.Enabled = true;
            timer.Elapsed += TimerElapsed;

            driverToken = new CancellationTokenSource();
            deviceResetEvent = new ManualResetEvent(false);
            driverThread = new Thread(() =>
            {
                while (!driverToken.IsCancellationRequested)
                {
                    var found = false;
                    var devices = HidDevices.Enumerate(VendorId);
                    foreach (var device in devices)
                    {
                        if (device != null)
                        {
                            device.ReadProduct(out var productBytes);
                            var product = ReadString(productBytes);

                            if (product.Equals(MouseName))
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    if (found != isCharging)
                    {
                        isCharging = found;
                        UpdateVisuals();
                    }

                    Thread.Sleep(FindDeviceSleepTime);
                }
            });
            driverThread.Start();
        }

        #endregion

        #region Private Methods

        private void Exit(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            driverToken.Cancel();
            deviceResetEvent.Set();
            driverThread.Join();
            Application.Exit();
        }

        private string ReadString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes).Replace("\0", string.Empty);
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (isCharging)
                currentBattery += BatteryIncreaseRate;
            else
                currentBattery -= BatteryDecreaseRate;

            currentBattery = Clamp(0, 100, currentBattery);

            UpdateVisuals();
            SaveBatteryPercentage();
        }

        private void UpdateVisuals()
        {
            var visualPercentage = Math.Round(currentBattery, 0);
            notifyIcon.Text = Resources.PanteraPRO + Environment.NewLine + (isCharging ? Resources.Battery_level_charging : string.Format(Resources.Battery_level, visualPercentage));
            notifyIcon.Icon = GetIcon();
        }

        private void LoadIcons()
        {
            battery100Icon = Icon.FromHandle(Resources.Battery100.GetHicon());
            battery75Icon = Icon.FromHandle(Resources.Battery75.GetHicon());
            battery50Icon = Icon.FromHandle(Resources.Battery50.GetHicon());
            battery25Icon = Icon.FromHandle(Resources.Battery25.GetHicon());
            batteryWarningIcon = Icon.FromHandle(Resources.BatteryWarning.GetHicon());
            batteryChargingIcon = Icon.FromHandle(Resources.BatteryCharging.GetHicon());
        }

        private Icon GetIcon()
        {
            if (isCharging && currentBattery < 100)
                return batteryChargingIcon;
            if (currentBattery >= 75)
                return battery100Icon;
            if (currentBattery >= 50)
                return battery75Icon;
            if (currentBattery >= 25)
                return battery50Icon;
            if (currentBattery >= 10)
                return battery25Icon;
            if (currentBattery >= 0)
                return batteryWarningIcon;
            return batteryChargingIcon;
        }

        private void SaveBatteryPercentage()
        {
            var settings = PanteraSettings.Load();
            settings.BatteryPercentage = currentBattery;
            settings.Save();
        }

        private float LoadBatteryPercentage()
        {
            var settings = PanteraSettings.Load();
            return settings.BatteryPercentage;
        }

        private float Clamp(float min, float max, float current)
        {
            if (current > max)
                return max;
            if (current < min)
                return min;
            return current;
        }

        #endregion
    }
}
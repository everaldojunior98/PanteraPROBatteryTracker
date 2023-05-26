using System;
using System.Drawing;
using System.Management;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using PanteraPRO.Properties;
using Timer = System.Timers.Timer;

namespace PanteraPRO
{
    public class AppContext : ApplicationContext
    {
        #region Fields

        private const int UpdateInterval = 60 * 1000;

        private const float BatteryDecreaseRate = 0.07f;
        private const float BatteryIncreaseRate = 1f;

        private const string MouseName = "USB\\VID_25A7&PID_FA7B\\5&356B5377&0&3";
        private const int FindDeviceSleepTime = 1000;

        private readonly NotifyIcon notifyIcon;

        private readonly ManualResetEvent deviceResetEvent;
        private readonly CancellationTokenSource driverToken;
        private readonly Thread driverThread;

        private float currentBattery;
        private bool isCharging;

        private Icon battery100Icon;
        private Icon battery90Icon;
        private Icon battery80Icon;
        private Icon battery70Icon;
        private Icon battery60Icon;
        private Icon battery50Icon;
        private Icon battery40Icon;
        private Icon battery30Icon;
        private Icon battery20Icon;
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

            var timer = new Timer(UpdateInterval);
            timer.Enabled = true;
            timer.Elapsed += TimerElapsed;

            driverToken = new CancellationTokenSource();
            deviceResetEvent = new ManualResetEvent(false);
            driverThread = new Thread(() =>
            {
                while (!driverToken.IsCancellationRequested)
                {
                    var found = false;
                    var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_USBHub");
                    foreach (var managementBaseObject in searcher.Get())
                    {
                        var obj = (ManagementObject) managementBaseObject;
                        foreach (var prop in obj.Properties)
                        {
                            if (prop.Name == "DeviceID" && prop.Value.ToString() == MouseName)
                            {
                                found = true;
                                break;
                            }
                        }

                        obj.Dispose();
                    }

                    searcher.Dispose();

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
            battery90Icon = Icon.FromHandle(Resources.Battery90.GetHicon());
            battery80Icon = Icon.FromHandle(Resources.Battery80.GetHicon());
            battery70Icon = Icon.FromHandle(Resources.Battery70.GetHicon());
            battery60Icon = Icon.FromHandle(Resources.Battery60.GetHicon());
            battery50Icon = Icon.FromHandle(Resources.Battery50.GetHicon());
            battery40Icon = Icon.FromHandle(Resources.Battery40.GetHicon());
            battery30Icon = Icon.FromHandle(Resources.Battery30.GetHicon());
            battery20Icon = Icon.FromHandle(Resources.Battery20.GetHicon());

            batteryWarningIcon = Icon.FromHandle(Resources.BatteryWarning.GetHicon());
            batteryChargingIcon = Icon.FromHandle(Resources.BatteryCharging.GetHicon());
        }

        private Icon GetIcon()
        {
            if (isCharging && currentBattery < 100)
                return batteryChargingIcon;
            if (currentBattery >= 90)
                return battery100Icon;
            if (currentBattery >= 80)
                return battery90Icon;
            if (currentBattery >= 70)
                return battery80Icon;
            if (currentBattery >= 60)
                return battery70Icon;
            if (currentBattery >= 50)
                return battery60Icon;
            if (currentBattery >= 40)
                return battery50Icon;
            if (currentBattery >= 30)
                return battery40Icon;
            if (currentBattery >= 20)
                return battery30Icon;
            if (currentBattery >= 10)
                return battery20Icon;
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
            try
            {
                var settings = PanteraSettings.Load();
                return settings.BatteryPercentage;
            }
            catch
            {
                currentBattery = 100;
                SaveBatteryPercentage();
            }

            return currentBattery;
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
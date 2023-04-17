using System;
using System.Drawing;
using System.Timers;
using System.Windows.Forms;
using PanteraPRO.Properties;
using Timer = System.Timers.Timer;

namespace PanteraPRO
{
    public class AppContext : ApplicationContext
    {
        #region Fields

        private const int UpdateInterval = 1000;

        private readonly NotifyIcon notifyIcon;
        private readonly Timer timer;

        private int count = 100;

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

            var exitMenuItem = new MenuItem(Resources.Exit, Exit);

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = GetIcon(count);
            notifyIcon.ContextMenu = new ContextMenu(new[] { exitMenuItem });
            notifyIcon.Visible = true;

            timer = new Timer(UpdateInterval);
            timer.Enabled = true;
            timer.Elapsed += TimerElapsed;
        }

        #endregion

        #region Private Methods

        private void Exit(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            count--;

            notifyIcon.Text = Resources.PanteraPRO + Environment.NewLine + (count > 0 ? string.Format(Resources.Battery_level, count) : Resources.Battery_level_charging);
            notifyIcon.Icon = GetIcon(count);
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

        private Icon GetIcon(int percentage)
        {
            if (percentage >= 75)
                return battery100Icon;
            if (percentage >= 50)
                return battery75Icon;
            if (percentage >= 25)
                return battery50Icon;
            if (percentage >= 10)
                return battery25Icon;
            if (percentage >= 0)
                return batteryWarningIcon;
            return batteryChargingIcon;
        }

        #endregion
    }
}
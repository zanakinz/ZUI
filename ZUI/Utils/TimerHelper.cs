using System;

namespace ZUI.Utils
{
    internal static class TimerHelper
    {
        public static void OneTickTimer(float interval, Action action)
        {
            var timer = new System.Timers.Timer(interval);
            timer.Elapsed += (sender, e) =>
            {
                action.Invoke();
                timer.Stop();
                timer.Dispose();
            };
            timer.AutoReset = false;
            timer.Enabled = true;
        }
    }
}

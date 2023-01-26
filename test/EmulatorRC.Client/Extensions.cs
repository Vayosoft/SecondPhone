using System.Runtime.CompilerServices;

namespace EmulatorRC.Client
{
    public static class Extensions
    {
        public static TaskAwaiter GetAwaiter(this TimeSpan time)
        {
            return Task.Delay(time).GetAwaiter();
        }

        public static TaskAwaiter GetAwaiter(this int milliseconds)
        {
            return Task.Delay(milliseconds).GetAwaiter();
        }

        public static TimeSpan Seconds(this int seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }
    }

}

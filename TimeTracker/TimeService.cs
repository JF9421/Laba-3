using System;
using System.Windows.Forms;

namespace TimeTracker
{
    public class TimerService
    {
        private Timer _timer;
        private TimeSpan _elapsed;
        public event Action<TimeSpan> OnTick;

        public TimerService()
        {
            _timer = new Timer { Interval = 1000 };
            _timer.Tick += (s, e) =>
            {
                _elapsed = _elapsed.Add(TimeSpan.FromSeconds(1));
                OnTick?.Invoke(_elapsed);
            };
        }

        public void Start() => _timer.Start();
        public void Pause() => _timer.Stop();

        public void Stop()
        {
            _timer.Stop();
            _elapsed = TimeSpan.Zero;
            OnTick?.Invoke(_elapsed);
        }

        public TimeSpan GetElapsed() => _elapsed;
    }
}
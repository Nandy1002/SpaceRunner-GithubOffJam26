using System;

namespace SpaceRunner.Core
{
    public sealed class RunTimer
    {
        private readonly PauseState pause;
        public bool IsRunning { get; private set; }
        public double ElapsedSeconds { get; private set; }

        public RunTimer(PauseState pause) => this.pause = pause;
        public void StartRun() { ElapsedSeconds = 0; IsRunning = true; }
        public void EndRun() => IsRunning = false;

        public void Tick(double seconds)
        {
            if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (IsRunning && !pause.IsPaused) ElapsedSeconds += seconds;
        }

        public string DisplayTime
        {
            get
            {
                var time = TimeSpan.FromSeconds(ElapsedSeconds);
                return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}.{time.Milliseconds / 10:00}";
            }
        }
    }
}

namespace DiningPhilosophers.Core.Threading
{
    public abstract class SimulationThread : IDisposable
    {
        private readonly Thread _thread;
        private bool _started;

        protected SimulationThread(CancellationToken cancellationToken, string? name = null, bool isBackground = true)
        {
            Cancellation = cancellationToken;
            _thread = new Thread(RunInternal)
            {
                IsBackground = isBackground,
                Name = name
            };
        }

        protected CancellationToken Cancellation { get; }

        private void RunInternal()
        {
            try
            {
                Execute();
            }
            catch (OperationCanceledException) when (Cancellation.IsCancellationRequested)
            {
                // Expected during shutdown, swallow to keep shutdown quiet.
            }
        }

        protected abstract void Execute();

        protected void SleepWithCancellation(int milliseconds)
        {
            if (milliseconds <= 0)
            {
                return;
            }

            Cancellation.WaitHandle.WaitOne(milliseconds);
        }

        public void Start()
        {
            if (_started)
            {
                throw new InvalidOperationException("Thread already started.");
            }
            _started = true;
            _thread.Start();
        }

        public void Join() => _thread.Join();

        public Thread ManagedThread => _thread;

        public void Dispose()
        {
            if (_thread.IsAlive)
            {
                try
                {
                    _thread.Join();
                }
                catch (ThreadStateException)
                {
                    // Thread was not started, ignore.
                }
            }
        }
    }
}

namespace DiningPhilosophers.Core.Entities
{
    public sealed class Fork
    {
        private readonly object _lockObj = new();
        private ForkUtils.ForkState _state = ForkUtils.ForkState.Available;
        private bool _isEating = false;
        private long _lastChangeMs = Environment.TickCount64;

        // Accumulated times in milliseconds
        private long _availableMs = 0;
        private long _queuedMs = 0;
        private long _inUseMs = 0;
        private long _inEatingMs = 0;

        public Fork(int id)
        {
            Id = id;
        }

        public int Id { get; }

        public string? Owner { get; private set; }

        public ForkUtils.ForkState State
        {
            get
            {
                lock (_lockObj)
                {
                    return _state;
                }
            }
        }

        // Expose accumulated times
        public long AvailableMs
        {
            get
            {
                lock (_lockObj)
                {
                    return _availableMs;
                }
            }
        }

        public long QueuedMs
        {
            get
            {
                lock (_lockObj)
                {
                    return _queuedMs;
                }
            }
        }

        public long InUseMs
        {
            get
            {
                lock (_lockObj)
                {
                    return _inUseMs;
                }
            }
        }

        public long InEatingMs
        {
            get
            {
                lock (_lockObj)
                {
                    return _inEatingMs;
                }
            }
        }

        private void AccumulateSinceLastChange()
        {
            var now = Environment.TickCount64;
            var delta = Math.Max(0, now - _lastChangeMs);
            switch (_state)
            {
                case ForkUtils.ForkState.Available:
                    _availableMs += delta;
                    break;
                case ForkUtils.ForkState.Queued:
                    _queuedMs += delta;
                    break;
                case ForkUtils.ForkState.InUse:
                    if (_isEating) _inEatingMs += delta;
                    else _inUseMs += delta;
                    break;
            }
        }

        /// <summary>
        /// Попытка взять вилку. Возвращает true, если вилка успешно взята.
        /// Этот метод потокобезопасен и использует внутреннюю блокировку.
        /// </summary>
        public bool TryTake(string philosopherName, int timeoutMs)
        {
            if (Monitor.TryEnter(_lockObj, timeoutMs))
            {
                try
                {
                    if (_state == ForkUtils.ForkState.Available)
                    {
                        Owner = philosopherName;
                        _isEating = false;
                        AccumulateSinceLastChange();
                        _state = ForkUtils.ForkState.InUse;
                        _lastChangeMs = Environment.TickCount64;
                        return true;
                    }
                    return false;
                }
                finally
                {
                    Monitor.Exit(_lockObj);
                }
            }
            return false;
        }

        /// <summary>
        /// Отметить, что философ начал есть с этой вилкой.
        /// Вызывается, когда философ держит обе вилки и начинает есть.
        /// </summary>
        public void MarkEating()
        {
            lock (_lockObj)
            {
                _isEating = true;
                AccumulateSinceLastChange();
                _lastChangeMs = Environment.TickCount64;
                _state = ForkUtils.ForkState.InUse;
            }
        }

        /// <summary>
        /// Освободить вилку. Возвращает true, если вилка была успешно освобождена.
        /// Этот метод потокобезопасен.
        /// </summary>
        public bool Release(string philosopherName)
        {
            lock (_lockObj)
            {
                if (Owner == philosopherName && _state == ForkUtils.ForkState.InUse)
                {
                    AccumulateSinceLastChange();
                    Owner = null;
                    _isEating = false;
                    _state = ForkUtils.ForkState.Available;
                    _lastChangeMs = Environment.TickCount64;
                    return true;
                }
                return false;
            }
        }
    }
}
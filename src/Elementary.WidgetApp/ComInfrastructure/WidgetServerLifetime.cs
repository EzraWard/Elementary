using System;
using System.Collections.Generic;
using System.Threading;

namespace Elementary.WidgetApp.ComInfrastructure
{
    internal sealed class WidgetServerLifetime : IDisposable
    {
        private readonly object _gate = new object();
        private readonly HashSet<string> _activeWidgetIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly TimeSpan _idleTimeout;
        private readonly EventWaitHandle _shutdownSignal = new EventWaitHandle(false, EventResetMode.ManualReset);
        private readonly Timer _idleTimer;

        public WidgetServerLifetime(TimeSpan idleTimeout)
        {
            _idleTimeout = idleTimeout;
            _idleTimer = new Timer(_ => _shutdownSignal.Set());
            ArmIdleShutdown();
        }

        public WaitHandle ShutdownSignal => _shutdownSignal;

        public void KeepAlive()
        {
            lock (_gate)
            {
                if (_activeWidgetIds.Count == 0)
                {
                    ArmIdleShutdown();
                }
            }
        }

        public void TrackWidget(string widgetId)
        {
            if (string.IsNullOrWhiteSpace(widgetId))
            {
                KeepAlive();
                return;
            }

            lock (_gate)
            {
                _activeWidgetIds.Add(widgetId);
                CancelIdleShutdown();
            }
        }

        public void UntrackWidget(string widgetId)
        {
            lock (_gate)
            {
                if (!string.IsNullOrWhiteSpace(widgetId))
                {
                    _activeWidgetIds.Remove(widgetId);
                }

                if (_activeWidgetIds.Count == 0)
                {
                    ArmIdleShutdown();
                }
            }
        }

        public void Dispose()
        {
            _idleTimer.Dispose();
            _shutdownSignal.Dispose();
        }

        private void ArmIdleShutdown()
        {
            _idleTimer.Change(_idleTimeout, Timeout.InfiniteTimeSpan);
        }

        private void CancelIdleShutdown()
        {
            _idleTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }
}

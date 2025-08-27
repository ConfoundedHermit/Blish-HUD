using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Blish_HUD._Utils {

    /// <summary>
    /// Provides automatic event subscription tracking and cleanup to prevent memory leaks.
    /// This utility helps manage event handler lifecycles and detect potential memory leaks.
    /// </summary>
    public class EventSubscriptionTracker : IDisposable {
        
        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();
        private readonly object _subscriptionsLock = new object();
        private bool _disposed = false;

        /// <summary>
        /// Tracks an event subscription for automatic cleanup during disposal.
        /// </summary>
        /// <typeparam name="T">The event args type</typeparam>
        /// <param name="source">The object that owns the event</param>
        /// <param name="eventName">The name of the event</param>
        /// <param name="handler">The event handler</param>
        /// <param name="callerMemberName">Automatically populated caller member name</param>
        /// <param name="callerFilePath">Automatically populated caller file path</param>
        /// <param name="callerLineNumber">Automatically populated caller line number</param>
        public void TrackSubscription<T>(object source, string eventName, EventHandler<T> handler,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0) where T : EventArgs {
            
            if (_disposed) {
                throw new ObjectDisposedException(nameof(EventSubscriptionTracker));
            }

            try {
                var eventInfo = source.GetType().GetEvent(eventName);
                if (eventInfo == null) {
                    // Use NLog logger directly since DebugService doesn't have WriteWarningLine
                    var logger = NLog.LogManager.GetLogger(nameof(EventSubscriptionTracker));
                    logger.Warn($"Event '{eventName}' not found on type '{source.GetType().Name}'");
                    return;
                }

                // Subscribe to the event
                eventInfo.AddEventHandler(source, handler);

                // Track the subscription for cleanup
                var subscription = new EventSubscription {
                    Source = new WeakReference(source),
                    EventName = eventName,
                    Handler = handler,
                    EventInfo = eventInfo,
                    SubscriptionTime = DateTime.UtcNow,
                    CallerMemberName = callerMemberName,
                    CallerFilePath = callerFilePath,
                    CallerLineNumber = callerLineNumber
                };

                lock (_subscriptionsLock) {
                    _subscriptions.Add(subscription);
                }

                #if DEBUG
                EventLeakDetector.TrackSubscription(this, $"{source.GetType().Name}.{eventName}");
                #endif

            } catch (Exception ex) {
                var logger = NLog.LogManager.GetLogger(nameof(EventSubscriptionTracker));
                logger.Error(ex, $"Failed to track event subscription: {source.GetType().Name}.{eventName}");
            }
        }

        /// <summary>
        /// Manually unsubscribe from a tracked event.
        /// </summary>
        /// <typeparam name="T">The event args type</typeparam>
        /// <param name="source">The object that owns the event</param>
        /// <param name="eventName">The name of the event</param>
        /// <param name="handler">The event handler</param>
        public void Unsubscribe<T>(object source, string eventName, EventHandler<T> handler) where T : EventArgs {
            if (_disposed) return;

            try {
                var eventInfo = source.GetType().GetEvent(eventName);
                eventInfo?.RemoveEventHandler(source, handler);

                // Remove from tracking
                lock (_subscriptionsLock) {
                    _subscriptions.RemoveAll(s => 
                        s.Source.Target == source && 
                        s.EventName == eventName && 
                        ReferenceEquals(s.Handler, handler));
                }

                #if DEBUG
                EventLeakDetector.TrackUnsubscription(this, $"{source.GetType().Name}.{eventName}");
                #endif

            } catch (Exception ex) {
                var logger = NLog.LogManager.GetLogger(nameof(EventSubscriptionTracker));
                logger.Error(ex, $"Failed to unsubscribe from event: {source.GetType().Name}.{eventName}");
            }
        }

        /// <summary>
        /// Gets diagnostic information about current subscriptions.
        /// </summary>
        public EventSubscriptionDiagnostics GetDiagnostics() {
            lock (_subscriptionsLock) {
                var activeSubscriptions = new List<EventSubscriptionInfo>();
                var deadReferences = 0;

                foreach (var subscription in _subscriptions) {
                    if (subscription.Source.Target != null) {
                        activeSubscriptions.Add(new EventSubscriptionInfo {
                            SourceType = subscription.Source.Target.GetType().Name,
                            EventName = subscription.EventName,
                            HandlerType = subscription.Handler.GetType().Name,
                            SubscriptionTime = subscription.SubscriptionTime,
                            CallerMemberName = subscription.CallerMemberName,
                            CallerFilePath = subscription.CallerFilePath,
                            CallerLineNumber = subscription.CallerLineNumber
                        });
                    } else {
                        deadReferences++;
                    }
                }

                return new EventSubscriptionDiagnostics {
                    ActiveSubscriptions = activeSubscriptions,
                    DeadReferences = deadReferences,
                    TotalTracked = _subscriptions.Count
                };
            }
        }

        /// <summary>
        /// Cleans up dead references from the subscription list.
        /// </summary>
        public void CleanupDeadReferences() {
            lock (_subscriptionsLock) {
                _subscriptions.RemoveAll(s => s.Source.Target == null);
            }
        }

        /// <summary>
        /// Disposes the tracker and unsubscribes from all tracked events.
        /// </summary>
        public void Dispose() {
            if (_disposed) return;

            lock (_subscriptionsLock) {
                foreach (var subscription in _subscriptions) {
                    try {
                        if (subscription.Source.Target != null) {
                            subscription.EventInfo.RemoveEventHandler(subscription.Source.Target, subscription.Handler);
                            
                            #if DEBUG
                            EventLeakDetector.TrackUnsubscription(this, $"{subscription.Source.Target.GetType().Name}.{subscription.EventName}");
                            #endif
                        }
                    } catch (Exception ex) {
                        // Log but don't throw during disposal
                        var logger = NLog.LogManager.GetLogger(nameof(EventSubscriptionTracker));
                        logger.Error(ex, $"Error during event cleanup: {subscription.EventName}");
                    }
                }

                _subscriptions.Clear();
            }

            _disposed = true;
        }

        private class EventSubscription {
            public WeakReference Source { get; set; }
            public string EventName { get; set; }
            public Delegate Handler { get; set; }
            public EventInfo EventInfo { get; set; }
            public DateTime SubscriptionTime { get; set; }
            public string CallerMemberName { get; set; }
            public string CallerFilePath { get; set; }
            public int CallerLineNumber { get; set; }
        }
    }

    /// <summary>
    /// Information about an active event subscription.
    /// </summary>
    public class EventSubscriptionInfo {
        public string SourceType { get; set; }
        public string EventName { get; set; }
        public string HandlerType { get; set; }
        public DateTime SubscriptionTime { get; set; }
        public string CallerMemberName { get; set; }
        public string CallerFilePath { get; set; }
        public int CallerLineNumber { get; set; }
    }

    /// <summary>
    /// Diagnostic information about event subscriptions.
    /// </summary>
    public class EventSubscriptionDiagnostics {
        public List<EventSubscriptionInfo> ActiveSubscriptions { get; set; } = new List<EventSubscriptionInfo>();
        public int DeadReferences { get; set; }
        public int TotalTracked { get; set; }
    }

    #if DEBUG
    /// <summary>
    /// Debug-only event leak detection utility.
    /// </summary>
    public static class EventLeakDetector {
        private static readonly ConcurrentDictionary<object, ConcurrentBag<string>> _subscriptions 
            = new ConcurrentDictionary<object, ConcurrentBag<string>>();
        
        private static readonly Timer _reportTimer = new Timer(ReportLeaks, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        public static void TrackSubscription(object subscriber, string eventName) {
            var events = _subscriptions.GetOrAdd(subscriber, _ => new ConcurrentBag<string>());
            events.Add(eventName);
        }

        public static void TrackUnsubscription(object subscriber, string eventName) {
            if (_subscriptions.TryGetValue(subscriber, out var events)) {
                // Note: ConcurrentBag doesn't support removal, so we track unsubscriptions separately
                events.Add($"UNSUBSCRIBED:{eventName}");
            }
        }

        private static void ReportLeaks(object state) {
            var potentialLeaks = new List<string>();

            foreach (var kvp in _subscriptions) {
                var subscriber = kvp.Key;
                var events = kvp.Value;
                
                if (subscriber == null) continue;

                var eventList = events.ToList();
                var subscribed = eventList.Where(e => !e.StartsWith("UNSUBSCRIBED:")).ToList();
                var unsubscribed = eventList.Where(e => e.StartsWith("UNSUBSCRIBED:"))
                                           .Select(e => e.Substring("UNSUBSCRIBED:".Length))
                                           .ToList();

                var activeSubscriptions = subscribed.Except(unsubscribed).ToList();
                
                if (activeSubscriptions.Count > 0) {
                    potentialLeaks.Add($"{subscriber.GetType().Name}: {string.Join(", ", activeSubscriptions)}");
                }
            }

            if (potentialLeaks.Count > 0) {
                var logger = NLog.LogManager.GetLogger(nameof(EventLeakDetector));
                logger.Warn($"Potential event leaks detected:\n{string.Join("\n", potentialLeaks)}");
            }
        }

        public static void ReportLeaks() {
            ReportLeaks(null);
        }
    }
    #endif

    /// <summary>
    /// Weak event manager for static events to prevent memory leaks.
    /// </summary>
    public static class WeakEventManager {
        private static readonly ConcurrentDictionary<string, ConcurrentBag<WeakReference>> _eventSubscriptions 
            = new ConcurrentDictionary<string, ConcurrentBag<WeakReference>>();

        private static readonly Timer _cleanupTimer = new Timer(CleanupDeadReferences, null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        /// <summary>
        /// Subscribe to a weak event that won't prevent garbage collection.
        /// </summary>
        /// <typeparam name="T">Event args type</typeparam>
        /// <param name="eventName">Name of the event</param>
        /// <param name="handler">Event handler</param>
        public static void Subscribe<T>(string eventName, EventHandler<T> handler) where T : EventArgs {
            var subscribers = _eventSubscriptions.GetOrAdd(eventName, _ => new ConcurrentBag<WeakReference>());
            subscribers.Add(new WeakReference(handler));
        }

        /// <summary>
        /// Raise a weak event to all living subscribers.
        /// </summary>
        /// <typeparam name="T">Event args type</typeparam>
        /// <param name="eventName">Name of the event</param>
        /// <param name="sender">Event sender</param>
        /// <param name="args">Event arguments</param>
        public static void RaiseEvent<T>(string eventName, object sender, T args) where T : EventArgs {
            if (!_eventSubscriptions.TryGetValue(eventName, out var subscribers)) return;

            var subscriberList = subscribers.ToArray();
            var aliveHandlers = new List<EventHandler<T>>();

            foreach (var weakRef in subscriberList) {
                if (weakRef.Target is EventHandler<T> handler) {
                    aliveHandlers.Add(handler);
                }
            }

            // Invoke all alive handlers
            foreach (var handler in aliveHandlers) {
                try {
                    handler(sender, args);
                } catch (Exception ex) {
                    var logger = NLog.LogManager.GetLogger(nameof(WeakEventManager));
                    logger.Error(ex, $"Error in weak event handler for {eventName}");
                }
            }
        }

        private static void CleanupDeadReferences(object state) {
            foreach (var kvp in _eventSubscriptions) {
                var eventName = kvp.Key;
                var subscribers = kvp.Value;
                
                var aliveSubscribers = subscribers.Where(wr => wr.Target != null).ToArray();
                
                if (aliveSubscribers.Length != subscribers.Count) {
                    // Replace the bag with only alive subscribers
                    var newBag = new ConcurrentBag<WeakReference>(aliveSubscribers);
                    _eventSubscriptions.TryUpdate(eventName, newBag, subscribers);
                }
            }
        }

        /// <summary>
        /// Gets statistics about weak event subscriptions.
        /// </summary>
        public static WeakEventStatistics GetStatistics() {
            var stats = new WeakEventStatistics();
            
            foreach (var kvp in _eventSubscriptions) {
                var eventName = kvp.Key;
                var subscribers = kvp.Value;
                
                var subscriberArray = subscribers.ToArray();
                var aliveCount = subscriberArray.Count(wr => wr.Target != null);
                var deadCount = subscriberArray.Length - aliveCount;
                
                stats.EventStats.Add(new WeakEventInfo {
                    EventName = eventName,
                    AliveSubscribers = aliveCount,
                    DeadReferences = deadCount
                });
                
                stats.TotalAliveSubscribers += aliveCount;
                stats.TotalDeadReferences += deadCount;
            }
            
            return stats;
        }
    }

    /// <summary>
    /// Statistics about weak event subscriptions.
    /// </summary>
    public class WeakEventStatistics {
        public List<WeakEventInfo> EventStats { get; set; } = new List<WeakEventInfo>();
        public int TotalAliveSubscribers { get; set; }
        public int TotalDeadReferences { get; set; }
    }

    /// <summary>
    /// Information about a specific weak event.
    /// </summary>
    public class WeakEventInfo {
        public string EventName { get; set; }
        public int AliveSubscribers { get; set; }
        public int DeadReferences { get; set; }
    }
}

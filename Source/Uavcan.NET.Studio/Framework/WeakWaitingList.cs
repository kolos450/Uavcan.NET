using System;
using System.Collections.Generic;
using System.Text;

namespace Uavcan.NET.Studio.Framework
{
    sealed class WeakWaitingList<K>
    {
        private readonly object _syncRoot = new();
        private readonly Predicate<K> _predicate;

        public WeakWaitingList(Predicate<K> predicate)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        Dictionary<K, List<(WeakReference Context, Action<object> Callback)>> _waitingList = new();

        public void WaitForKey(K key, object context, Action<object> callback)
        {
            if (callback is null)
                throw new ArgumentNullException(nameof(callback));

            bool executeCallback = false;
            lock (_syncRoot)
            {
                if (_predicate(key))
                {
                    executeCallback = true;
                }
                else
                {
                    CleanupWaitingList();

                    if (!_waitingList.TryGetValue(key, out var bag))
                    {
                        bag = new();
                        _waitingList.Add(key, bag);
                    }

                    bag.Add((new WeakReference(context), callback));
                }
            }

            if (executeCallback)
            {
                callback(context);
            }
        }

        private void CleanupWaitingList()
        {
            List<K> keysToRemove = null;

            foreach (var kv in _waitingList)
            {
                List<int> valuesToRemove = null;

                var bag = kv.Value;
                for (int i = 0; i < bag.Count; i++)
                {
                    if (!bag[i].Context.IsAlive)
                    {
                        (valuesToRemove ??= new()).Add(i);
                    }
                }

                if (valuesToRemove is not null)
                {
                    for (int i = valuesToRemove.Count - 1; i >= 0; i--)
                    {
                        bag.RemoveAt(i);
                    }
                }

                if (bag.Count == 0)
                {
                    (keysToRemove ??= new List<K>()).Add(kv.Key);
                }
            }

            if (keysToRemove is not null)
            {
                foreach (var key in keysToRemove)
                {
                    _waitingList.Remove(key);
                }
            }
        }

        public void AddKey(K key)
        {
            List<(WeakReference Context, Action<object> Callback)> waitingList;
            lock (_syncRoot)
            {
                if (_waitingList.TryGetValue(key, out waitingList))
                    _waitingList.Remove(key);
            }

            if (waitingList is not null)
            {
                foreach (var (context, callback) in waitingList)
                {
                    if (context.IsAlive)
                    {
                        callback(context.Target);
                    }
                }
            }
        }
    }
}

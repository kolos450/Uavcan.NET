using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uavcan.NET.Studio.Communication
{
    public sealed class TaskBag : IDisposable
    {
        private CancellationTokenSource _cts = new();
        private LinkedList<Task> _tasks = new();
        private readonly object _syncRoot = new();

        public CancellationToken CancellationToken => _cts.Token;

        public void Dispose()
        {
            if (_cts is not null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            if (_tasks is not null)
            {
                try
                {
                    Task.WhenAll(_tasks.ToArray()).GetAwaiter().GetResult();
                }
                catch (TaskCanceledException) { }

                _tasks = null;
            }
        }

        public void Add(Task task)
        {
            lock (_syncRoot)
            {
                _tasks.AddLast(task);
            }
        }

        public void Add(Func<Task> func) =>
            Add(func());

        public void FinalizeCompletedTasks()
        {
            if (_tasks.Count == 0)
                return;

            lock (_syncRoot)
            {
                var node = _tasks.First;
                while (node != null)
                {
                    var next = node.Next;
                    if (node.Value.IsCompleted)
                    {
                        node.Value.GetAwaiter().GetResult();
                        _tasks.Remove(node);
                    }
                    node = next;
                }
            }
        }
    }
}

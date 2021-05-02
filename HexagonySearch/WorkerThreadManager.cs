using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HexagonySearch
{
    internal class WorkerThreadManager
    {
        private readonly Action<int> _func;
        private readonly List<Thread> _threads;
        private readonly int _workItemCount;
        private int _index = -1;

        public WorkerThreadManager(int workerThreadCount, int workItemCount, Action<int> func)
        {
            _func = func;
            _workItemCount = workItemCount;
            _threads = Enumerable.Range(0, workerThreadCount)
                .Select(x => new Thread(Worker) { Name = $"Worker Thread {x}" })
                .ToList();
        }

        public void Run()
        {
            _threads.ForEach(x => x.Start());
            _threads.ForEach(x => x.Join());
        }

        private void Worker()
        {
            while (true)
            {
                var value = Interlocked.Increment(ref _index);
                if (value >= _workItemCount)
                {
                    break;
                }

                _func(value);
            }
        }
    }
}

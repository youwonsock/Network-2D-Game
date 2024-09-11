using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public interface IJobQueue
    {
        void Push(Action job);
    }

    public class JobQueue : IJobQueue
    {
        Queue<Action> jobQueue = new Queue<Action>();
        object lockObj = new object();
        bool flush = false;

        public void Push(Action job)
        {
            bool tempFlush = false;

            lock (lockObj)
            {
                jobQueue.Enqueue(job);
                
                if (flush == false)
                    tempFlush = flush = true;
            }

            if(tempFlush)
                Flush();
        }

        private Action Pop()
        {
            lock (lockObj)
            {
                if (jobQueue.Count == 0)
                {
                    flush = false;
                    return null;
                }

                return jobQueue.Dequeue();
            }
        }

        private void Flush()
        {
            while (true)
            {
                Action action = Pop();
                if (action == null)
                    return;

                action.Invoke();
            }
        }
    }
}

using System;
using ServerCore;

namespace Server.Game
{
    struct JobTimerElem : IComparable<JobTimerElem>
    {
        public int execTick;
        public IJob job;

        public int CompareTo(JobTimerElem other)
        {
            return other.execTick - execTick;
        }
    }

    public class JobTimer
    {
        PriorityQueue<JobTimerElem> heap = new PriorityQueue<JobTimerElem>();
        object lockObj = new object();

        public void Push(IJob job, int tickAfter = 0)
        {
            JobTimerElem jobElement;
            jobElement.execTick = System.Environment.TickCount + tickAfter;
            jobElement.job = job;

            lock (lockObj)
            {
                heap.Push(jobElement);
            }
        }

        public void Flush()
        {
            while (true)
            {
                int now = System.Environment.TickCount;

                JobTimerElem jobElement;

                lock (lockObj)
                {
                    if (heap.Count == 0)
                        break;

                    jobElement = heap.Peek();
                    if (jobElement.execTick > now)
                        break;

                    heap.Pop();
                }

                jobElement.job.Execute();
            }
        }
    }
}

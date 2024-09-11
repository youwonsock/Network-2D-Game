using ServerCore;

namespace Server
{
    struct JobTimerElem : IComparable<JobTimerElem>
    {
        public int execTime; // 지연 시간
        public Action action;

        public int CompareTo(JobTimerElem other)
        {
            return other.execTime - execTime;
        }
    }

    class JobTimer
    {
        PriorityQueue<JobTimerElem> heap = new PriorityQueue<JobTimerElem>();
        object lockObj = new object();



        public static JobTimer Instance { get; } = new JobTimer();



        public void Push(Action action, int tickAfter = 0)
        {
            JobTimerElem elem = new JobTimerElem();
            elem.execTime = System.Environment.TickCount + tickAfter;
            elem.action = action;

            lock (lockObj)
            {
                heap.Push(elem);
            }
        }

        public void Flush()
        {
            while (true)
            {
                int now = System.Environment.TickCount;

                JobTimerElem job;

                lock (lockObj)
                {
                    if (heap.Count == 0)
                        break;

                    job = heap.Peek();
                    if (job.execTime > now)
                        break;

                    heap.Pop();
                }

                job.action.Invoke();
            }
        }
    }
}

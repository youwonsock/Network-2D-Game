using System;
using ServerCore;

namespace Server.Game
{
	struct JobTimerElem : IComparable<JobTimerElem>
	{
		public int execTick;    // execution time
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



        public void Push(int tickAfter, Action action) { Push(tickAfter, new Job(action)); }
        public void Push<T1>(int tickAfter, Action<T1> action, T1 t1) { Push(tickAfter, new Job<T1>(action, t1)); }
        public void Push<T1, T2>(int tickAfter, Action<T1, T2> action, T1 t1, T2 t2) { Push(tickAfter, new Job<T1, T2>(action, t1, t2)); }
        public void Push<T1, T2, T3>(int tickAfter, Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) { Push(tickAfter, new Job<T1, T2, T3>(action, t1, t2, t3)); }



		public void Push(int tickAfter, IJob job)
		{
			JobTimerElem jobElement;
			jobElement.execTick = System.Environment.TickCount + tickAfter; // calculate the execution time
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
				JobTimerElem jobElement;

				lock (lockObj)
				{
					if (heap.Count == 0)
						break;

                    jobElement = heap.Peek();       // get the job with the smallest execution time
                    if (jobElement.execTick > System.Environment.TickCount)  // if the execution time is not reached yet
						return;					

                    heap.Pop();
				}

				jobElement.job.Execute();
			}
		}
	}
}

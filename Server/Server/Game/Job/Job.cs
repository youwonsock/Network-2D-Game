using System;

namespace Server.Game
{
	public abstract class IJob
	{
		public abstract void Execute();
        public bool Cancel { get; set; } = false;
    }

	public class Job : IJob
	{
		Action action;

		public Job(Action action)
		{
			this.action = action;
		}

		public override void Execute()
		{
			if(Cancel == false)	
				action.Invoke();
		}
	}

	public class Job<T1> : IJob
	{
		Action<T1> action;
		T1 t1;

		public Job(Action<T1> action, T1 t1)
		{
			this.action = action;
			this.t1 = t1;
		}

		public override void Execute()
        {
            if (Cancel == false)
                action.Invoke(t1);
		}
	}

	public class Job<T1, T2> : IJob
	{
		Action<T1, T2> action;
		T1 t1;
		T2 t2;

		public Job(Action<T1, T2> action, T1 t1, T2 t2)
		{
			this.action = action;
			this.t1 = t1;
			this.t2 = t2;
		}

		public override void Execute()
        {
            if (Cancel == false)
                action.Invoke(t1, t2);
		}
	}

	public class Job<T1, T2, T3> : IJob
	{
		Action<T1, T2, T3> action;
		T1 t1;
		T2 t2;
		T3 t3;

		public Job(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3)
		{
			this.action = action;
			this.t1 = t1;
			this.t2 = t2;
			this.t3 = t3;
		}

		public override void Execute()
        {
            if (Cancel == false)
                action.Invoke(t1, t2, t3);
		}
	}
}

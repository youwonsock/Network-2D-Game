# Network-2D-Game

## Developer Info
* 유원석(You Won Sock)
* GitHub : https://github.com/youwonsock
* Mail : qazwsx233434@gmail.com

### Development kits

<p>
<img src="https://upload.wikimedia.org/wikipedia/commons/thumb/1/19/Unity_Technologies_logo.svg/1280px-Unity_Technologies_logo.svg.png" height="40">
</p>

<p>
<img src="https://upload.wikimedia.org/wikipedia/commons/thumb/7/7d/Microsoft_.NET_logo.svg/640px-Microsoft_.NET_logo.svg.png" height="40">
</p>

<b><h2>Periods</h2></b>

* 2024-08 - 2023-09

## Contribution

![ui](https://github.com/user-attachments/assets/d52da7ff-466c-4cf0-98e2-e5fd031620c8)

### PacketGenerator
Google protobuf를 사용해 패킷 직렬화 및 역직렬화를 하였습니다.

이때 사용되는 protobuf 포맷을 기반으로 PacketGenerator클래스를 만들었습니다.

``` c#
using System.IO;

namespace PacketGenerator
{
	class PacketGenerator
    {
        static string clientRegister;
        static string serverRegister;

        static void Main(string[] args)
        {
            string file = "../../../Common/protoc-28.2-win64/bin/Protocol.proto";
            if (args.Length >= 1)
                file = args[0];

            bool startParsing = false;
            foreach (string line in File.ReadAllLines(file))
            {
                if (!startParsing && line.Contains("enum MsgId"))
                {
                    startParsing = true;
                    continue;
                }

                if (!startParsing)
                    continue;

                if (line.Contains("}"))
                    break;

                string[] names = line.Trim().Split(" =");
                if (names.Length == 0)
                    continue;

                string name = names[0];
                if (name.StartsWith("S_"))
                {
                    string[] words = name.Split("_");

                    string msgName = "";
                    foreach (string word in words)
                        msgName += FirstCharToUpper(word);

                    string packetName = $"S_{msgName.Substring(1)}";
                    clientRegister += string.Format(PacketFormat.managerRegisterFormat, msgName, packetName);
                }
                else if (name.StartsWith("C_"))
                {
                    string[] words = name.Split("_");

                    string msgName = "";
                    foreach (string word in words)
                        msgName += FirstCharToUpper(word);

                    string packetName = $"C_{msgName.Substring(1)}";
                    serverRegister += string.Format(PacketFormat.managerRegisterFormat, msgName, packetName);
                }
            }

            string clientManagerText = string.Format(PacketFormat.managerFormat, clientRegister);
            File.WriteAllText("ClientPacketManager.cs", clientManagerText);
            string serverManagerText = string.Format(PacketFormat.managerFormat, serverRegister);
            File.WriteAllText("ServerPacketManager.cs", serverManagerText);
        }

        public static string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";
            return input[0].ToString().ToUpper() + input.Substring(1).ToLower();
        }
    }
}
```

### Server
Command패턴을 사용한 JobTimer를 구현하였습니다.

JobTimer 클래스는 heap을 사용해 job의 우선순위를 관리하며 일정 주기마다 
실행시간을 확인하여 시간이 지난 job을 실행합니다.

```c#
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

```

  

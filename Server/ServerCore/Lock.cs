using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    // 현재 버전은 아직 재귀적 락에서 WriteLock -> ReadLock으로의 획득 후
    // WriteLock을 먼저 반환하고 ReadLock을 반환할 때 문제가 발생할 수 있다.

    // 재귀적 락 허용 : WriteLock -> WriteLock, WriteLock -> ReadLock
    // 스핀락 정책 (5000번 -> Yield)
    class Lock
    {
        const int EMPTY_FLAG = 0x00000000;
        const int WRITE_MASK = 0x7FFF0000;
        const int READ_MASK = 0x0000FFFF;
        const int MAX_SPIN_COUNT = 5000;

        // 비트 구분 : [unused(1)] [WriteThreadID(15)] [ReadCount(16)]
        int flag = EMPTY_FLAG;
        int writeCount = 0;
        

        public void WriteLock()
        {
            // 이미 WriteLock을 획득한 Thread가 다시 WriteLock을 획득할 때
            int lockThreadId = (flag & WRITE_MASK) >> 16;
            if(Thread.CurrentThread.ManagedThreadId == lockThreadId)
            {
                writeCount++;
                return;
            }

            // 아무도 WriteLock or ReadLock을 획득하고 있지 않을 때 경합해서 획득
            int desired = (Thread.CurrentThread.ManagedThreadId << 16) & WRITE_MASK; // 현재 Thread의 ThreadID를 추출
            while (true)
            {
                for (int i = 0; i < MAX_SPIN_COUNT; i++)
                {
                    // flag가 EMPTY_FLAG(Write 중이 아닌)일 때 desired 값을 적용
                    if (Interlocked.CompareExchange(ref flag, desired, EMPTY_FLAG) == EMPTY_FLAG)
                    {
                        writeCount = 1;
                        return;
                    }
                }

                Thread.Yield();
            }
        }

        public void WriteUnlock()
        {
            if(--writeCount == 0)
                Interlocked.Exchange(ref flag, EMPTY_FLAG);
        }

        public void ReadLock()
        {
            // 이미 WriteLock을 획득한 Thread가 ReadLock을 획득할 때
            int lockThreadId = (flag & WRITE_MASK) >> 16;
            if (Thread.CurrentThread.ManagedThreadId == lockThreadId)
            {
                Interlocked.Increment(ref flag);
                return;
            }

            // 아무도 WriteLock을 획득하고 있지 않을 때 ReadCount를 1씩 증가
            while (true)
            {
                for (int i = 0; i < MAX_SPIN_COUNT; i++)
                {
                    int expected = (flag & READ_MASK);

                    if (Interlocked.CompareExchange(ref flag, expected + 1, expected) == expected)  // ReadCount를 정상적으로 증가시키기 위해서
                        return;
                }

                Thread.Yield();
            }
        }

        public void ReadUnlock()
        {
            Interlocked.Decrement(ref flag);
        }
    }
}

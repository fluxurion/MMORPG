﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MMORPG.Common.Tool
{
    public class TimeWheel
    {
        private const int CircleCount = 5;
        private const int SlotCount = 1 << 6;

        public struct TimeTask
        {
            public int Tick;
            public Action<TimeTask> Action;
            public LinkedListNode<TimeTask> LinkedListNode;
        };

        private LinkedList<TimeTask> _addList;
        private LinkedList<TimeTask> _backupAddList;
        private List<TimeTask> _removeList;
        private List<TimeTask> _backupRemoveList;
        private LinkedList<TimeTask>[] _slot;
        private int[] _indexArr;

        private long _lastMs;
        private int _tickMs;  // The minimum slot time range, in milliseconds

        private bool _stop;

        public TimeWheel(int tickMs = 10) {

            _addList = new();
            _backupAddList = new();
            _removeList = new();
            _backupRemoveList = new();
            _slot = new LinkedList<TimeTask>[SlotCount * CircleCount];
            _indexArr = new int[CircleCount];
            _tickMs = tickMs;
            _stop = false;

            for (int i = 0; i < SlotCount * CircleCount; i++)
            {
                _slot[i] = new();
            }
            for (int i = 0; i < CircleCount; i++)
            {
                _indexArr[i] = 0;
            }
        }

        public async Task Start()
        {
            _lastMs = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond / _tickMs;
            do
            {
                if (_addList.Count > 0)
                {
                    lock (_addList)
                    {
                        (_backupAddList, _addList) = (_addList, _backupAddList);
                    }
                    DispatchTasksToSlot(_backupAddList);
                }
                if (_removeList.Count > 0)
                {
                    lock (_removeList)
                    {
                        (_backupRemoveList, _removeList) = (_removeList, _backupRemoveList);
                    }
                    for (int i = 0; i < _backupRemoveList.Count; i++)
                    {
                        var node = _backupRemoveList[i].LinkedListNode;
                        var list = node.List;
                        list?.Remove(node);
                    }
                    _backupRemoveList.Clear();
                }

                // Advances the time frame by frame based on the time elapsed since the last cycle.
                var nowMs = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond / _tickMs;
                var duration = nowMs - _lastMs;
                for (int i = 0; i < duration; i++)
                {
                    if (_slot[_indexArr[0]].Count > 0)
                    {
                        foreach (var task in _slot[_indexArr[0]])
                        {
                            try
                            {
                                task.Action(task);
                            }
                            catch
                            {
                                // ignored
                            }
                            // Task.Cast(task.Action);
                        }
                        _slot[_indexArr[0]].Clear();
                    }

                    // If it is necessary to advance to the upper time wheel, the queue of the upper time wheel will be dispatched downwards
                    int j = 0;
                    ++_indexArr[0];
                    do
                    {
                        bool ge = _indexArr[j] >= SlotCount;
                        if (ge) _indexArr[j] = 0;
                        if (j > 0)
                        {
                            int index = j * SlotCount + _indexArr[j];
                            DispatchTasksToSlot(_slot[index]);
                        }
                        if (!ge || ++j >= CircleCount) break;
                        ++_indexArr[j];
                    } while (true);
                }
                _lastMs = nowMs;
                await Task.Delay(_tickMs);
            } while (_stop == false);
        }

        void Stop()
        {
            _stop = true;
        }

        private int GetLayerByTick(int tick)
        {
            const int mask = 0x3f; // 0011 1111
            for (int i = 0; i < CircleCount; i++)
            {
                if ((tick & ~mask) == 0)
                {
                    return i;
                }
                tick >>= 6;
            }
            throw new Exception("TimeWheel.GetLayerByTick: Tick too large.");
        }

        private void DispatchTasksToSlot(LinkedList<TimeTask> list)
        {
            for (var task = list.First; task != null; )
            {
                var next = task.Next;

                // Insert into the corresponding slot according to the duration
                int tick = task.Value.Tick;

                int layer = GetLayerByTick(tick);
                int index = tick & 0x3f;
                index = layer * SlotCount + ((index + _indexArr[layer]) % SlotCount);

                // Clear the duration of the current layer so that it can be inserted into the next layer when it is dispatched downwards next time
                int mask2 = ~(0x7fffffff << (layer * 6));
                task.ValueRef.Tick &= mask2;

                list.Remove(task);
                _slot[index].AddLast(task);

                task = next;
            }
        }

        /// <summary>
        /// Asynchronously append a delayed task to the timer.
        /// The returned task cannot be modified.
        /// </summary>
        public TimeTask AddTask(int ms, Action<TimeTask> action)
        {
            if (ms < _tickMs) {
                ms = _tickMs;
            }
            var task = new TimeTask()
            {
                Tick = ms / _tickMs,
                Action = action,
            };
            lock (_addList)
            {
                var node = _addList.AddLast(task);
                task.LinkedListNode = node;
            }
            return task;
        }

        /// <summary>
        /// Asynchronously delete delayed tasks
        /// </summary>
        /// <param name="task"></param>
        public void RemoveTask(TimeTask task)
        {
            lock (_removeList)
            {
                _removeList.Add(task);
            }
        }
    }

    public class TimeWheelTest
    {
        public static async Task Start()
        {
            //Console.WriteLine($"start:{DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond}");
            int count = 0;
            var tw = new TimeWheel(1);
            tw.Start();
            var task = tw.AddTask(1, (task) => {
                Console.WriteLine($"hello");
            });
            await Task.Delay(1000);
            tw.RemoveTask(task);


            var begin = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
           
            var randomNumber = new byte[4];
            for (int i = 0; i < 1000000; i++)
            {
                //using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                //{
                //    rng.GetBytes(randomNumber);
                //}
                //int randomValue = BitConverter.ToInt32(randomNumber, 0);
                //randomValue = Math.Abs(randomValue);
                //randomValue %= 10;

                int j = i;
                tw.AddTask(1, (task) => {
                    //Console.WriteLine($"[{j}][{count++}]{randomValue}ms:{DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond}");
                    //count++;
                    Interlocked.Increment(ref count);
                });
            }
            while (count < 1000000)
            {
                await Task.Delay(1);
            }
            var end = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            //await Task.Delay(1000000);
            Console.WriteLine($"end:{end - begin}ms");
        }
    }

}

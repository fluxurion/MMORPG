using System;
using System.Collections.Generic;

namespace AOI
{
    /// <summary>
    /// 1、Object pool version, can effectively avoid GC.
    /// 2、If you do not have very strict requirements for GC, it is recommended not to use this version.
    /// 3、Because if the amount of data is too large, it will cause excessive memory usage, and in severe cases it may even cause heap overflow.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SkipListPool<T>
    {
        private int _level;
        private SkipListNode<T> _header;
        private readonly Random _random = new Random();
        private readonly Queue<SkipListNode<T>> _pool= new Queue<SkipListNode<T>>();

        public void Add(long target, T obj)
        {
            var rLevel = 1;
            while (rLevel <= _level && _random.Next(2) == 0) ++rLevel;

            if (rLevel > _level)
            {
                _level = rLevel;
                
                _header = Fetch().Init(target, obj, null, _header);
            }

            SkipListNode<T> cur = _header, last = null;

            for (var l = _level; l >= 1; --l)
            {
                while (cur.Right != null && cur.Right.Value < target) cur = cur.Right;

                if (l <= rLevel)
                {
                    cur.Right = Fetch().Init(target, obj, cur.Right, null);

                    if (last != null) last.Down = cur.Right;

                    last = cur.Right;
                }

                cur = cur.Down;
            }
        }

        public bool TryGetValue(long target, out SkipListNode<T> node)
        {
            node = null;

            var cur = _header;

            while (cur != null)
            {
                while (cur.Right != null && cur.Right.Value < target) cur = cur.Right;

                if (cur.Right != null && cur.Right.Value == target)
                {
                    node = cur.Right;
                    while (node.Down != null) node = node.Down;
                    return true;
                }

                cur = cur.Down;
            }

            return false;
        }

        public bool Remove(long target, out T obj)
        {
            var cur = _header;
            obj = default;
            var seen = false;

            for (var l = _level; l >= 1; --l)
            {
                while (cur.Right != null && cur.Right.Value < target) cur = cur.Right;

                if (cur.Right != null && cur.Right.Value == target)
                {
                    var temp = cur.Right;
                    obj = temp.Obj;
                    cur.Right = cur.Right.Right;
                    Recycle(temp);
                    seen = true;
                }

                cur = cur.Down;
            }

            return seen;
        }
        
        private SkipListNode<T> Fetch()
        {
            return _pool.Count <= 0 ? new SkipListNode<T>() : _pool.Dequeue();
        }

        private void Recycle(SkipListNode<T> node)
        {
            node.Obj = default;

            _pool.Enqueue(node);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AOI.Old
{
    public class AoiComponent
    {
        private readonly Dictionary<long, AoiNode> _nodes = new Dictionary<long, AoiNode>();

        private readonly AoiNodeLinkedList _xLinks = new AoiNodeLinkedList(10, AoiNodeLinkedListType.XLink);
        
        private readonly AoiNodeLinkedList _yLinks = new AoiNodeLinkedList(10, AoiNodeLinkedListType.YLink);

        public void Awake(){}

        /// <summary>
        /// Newly added to AOI
        /// </summary>
        /// <param name="id">Usually the character's ID or other identification ID</param>
        /// <param name="x">X-axis position</param>
        /// <param name="y">Y-axis position</param>
        /// <returns></returns>
        public AoiNode Enter(long id, float x, float y)
        {
            if (_nodes.TryGetValue(id, out var node)) return node;

            node = Pool<AoiNode>.Rent().Init(id, x, y);;

            _xLinks.Insert(node);

            _yLinks.Insert(node);

            _nodes[node.Id] = node;

            return node;
        }

        /// <summary>
        /// Update node
        /// </summary>
        /// <param name="id">Usually the character's ID or other identification ID</param>
        /// <param name="area">area distance</param>
        /// <param name="x">X-axis position</param>
        /// <param name="y">Y-axis position</param>
        /// <returns></returns>
        public AoiNode Update(long id, Vector2 area, float x, float y)
        {
            return !_nodes.TryGetValue(id, out var node) ? null : Update(node, area, x, y);
        }

        /// <summary>
        /// Update node
        /// </summary>
        /// <param name="node">Aoi node</param>
        /// <param name="area">area distance</param>
        /// <param name="x">X-axis position</param>
        /// <param name="y">Y-axis position</param>
        /// <returns></returns>
        public AoiNode Update(AoiNode node, Vector2 area, float x, float y)
        {
            // Transfer the new AOI node to the old node

            node.AoiInfo.MoveOnlySet = node.AoiInfo.MovesSet.Select(d => d).ToHashSet();

            // Move to new location

            Move(node, x, y);

            // Find surrounding coordinates

            Find(node, area);

            // Difference calculation

            node.AoiInfo.EntersSet = node.AoiInfo.MovesSet.Except(node.AoiInfo.MoveOnlySet).ToHashSet();

            // People who added themselves to the entry point

            foreach (var enterNode in node.AoiInfo.EntersSet) GetNode(enterNode).AoiInfo.MovesSet.Add(node.Id);

            node.AoiInfo.LeavesSet = node.AoiInfo.MoveOnlySet.Except(node.AoiInfo.MovesSet).ToHashSet();

            node.AoiInfo.MoveOnlySet = node.AoiInfo.MoveOnlySet.Except(node.AoiInfo.EntersSet)
                .Except(node.AoiInfo.LeavesSet).ToHashSet();

            return node;
        }

        public AoiNode Update(AoiNode node, Vector2 area)
        {
            return Update(node, area, node.Position.X, node.Position.Y);
        }

        /// <summary>
        /// move
        /// </summary>
        /// <param name="node">Aoi node</param>
        /// <param name="x">X-axis position</param>
        /// <param name="y">Y-axis position</param>
        private void Move(AoiNode node, float x, float y)
        {
            #region Move X axis

            if (Math.Abs(node.Position.X - x) > 0)
            {
                if (x > node.Position.X)
                {
                    var cur = node.Link.XNode.Next;

                    while (cur != null)
                    {
                        if (x < cur.Value.Position.X)
                        {
                            _xLinks.Remove(node.Link.XNode);

                            node.Position.X = x;
                            
                            node.Link.XNode = _xLinks.AddBefore(cur, node);

                            break;
                        }
                        else if (cur.Next == null)
                        {
                            _xLinks.Remove(node.Link.XNode);
                            
                            node.Position.X = x;
                            
                            node.Link.XNode = _xLinks.AddAfter(cur, node);

                            break;
                        }

                        cur = cur.Next;
                    }
                }
                else
                {
                    var cur = node.Link.XNode.Previous;

                    while (cur != null)
                    {
                        if (x > cur.Value.Position.X)
                        {
                            _xLinks.Remove(node.Link.XNode);
                            
                            node.Position.X = x;
                            
                            node.Link.XNode = _xLinks.AddAfter(cur, node);

                            break;
                        }
                        else if (cur.Previous == null)
                        {
                            _xLinks.Remove(node.Link.XNode);
                            
                            node.Position.X = x;
                            
                            node.Link.XNode = _xLinks.AddAfter(cur, node);

                            break;
                        }

                        cur = cur.Previous;
                    }
                }
            }

            #endregion

            #region Move Y axis

            if (Math.Abs(node.Position.Y - y) > 0)
            {
                if (y > node.Position.Y)
                {
                    var cur = node.Link.YNode.Next;

                    while (cur != null)
                    {
                        if (y < cur.Value.Position.Y)
                        {
                            _yLinks.Remove(node.Link.YNode);
                            
                            node.Position.Y = y;
                            
                            node.Link.YNode = _yLinks.AddBefore(cur, node);

                            break;
                        }
                        else if (cur.Next == null)
                        {
                            _yLinks.Remove(node.Link.YNode);
                            
                            node.Position.Y = y;
                            
                            node.Link.YNode = _yLinks.AddAfter(cur, node);

                            break;
                        }

                        cur = cur.Next;
                    }
                }
                else
                {
                    var cur = node.Link.YNode.Previous;

                    while (cur != null)
                    {
                        if (y > cur.Value.Position.Y)
                        {
                            _yLinks.Remove(node.Link.YNode);
                            
                            node.Position.Y = y;
                            
                            node.Link.YNode = _yLinks.AddBefore(cur, node);

                            break;
                        }
                        else if (cur.Previous == null)
                        {
                            _yLinks.Remove(node.Link.YNode);
                            
                            node.Position.Y = y;
                            
                            node.Link.YNode = _yLinks.AddAfter(cur, node);

                            break;
                        }

                        cur = cur.Previous;
                    }
                }
            }

            
            #endregion

            node.SetPosition(x, y);
        }

        /// <summary>
        /// Find surrounding coordinates based on the specified range
        /// </summary>
        /// <param name="id">Usually the character's ID or other identification ID</param>
        /// <param name="area">area distance</param>
        public AoiNode Find(long id, Vector2 area)
        {
            return !_nodes.TryGetValue(id, out var node) ? null : Find(node, area);
        }

        /// <summary>
        /// Find surrounding coordinates based on the specified range
        /// </summary>
        /// <param name="node">Aoi node</param>
        /// <param name="area">area distance</param>
        public AoiNode Find(AoiNode node, Vector2 area)
        {
            node.AoiInfo.MovesSet.Clear();
            
            for (var i = 0; i < 2; i++)
            {
                var cur = i == 0 ? node.Link.XNode.Next : node.Link.XNode.Previous;

                while (cur != null)
                {
                    if (Math.Abs(Math.Abs(cur.Value.Position.X) - Math.Abs(node.Position.X)) > area.X)
                    {
                        break;
                    }
                    else if (Math.Abs(Math.Abs(cur.Value.Position.Y) - Math.Abs(node.Position.Y)) <= area.Y)
                    {
                        if (Distance(node.Position, cur.Value.Position) <= area.X)
                        {
                            if (!node.AoiInfo.MovesSet.Contains(cur.Value.Id)) node.AoiInfo.MovesSet.Add(cur.Value.Id);
                        }
                    }

                    cur = i == 0 ? cur.Next : cur.Previous;
                }
            }

            for (var i = 0; i < 2; i++)
            {
               var cur = i == 0 ? node.Link.YNode.Next : node.Link.YNode.Previous;

                while (cur != null)
                {
                    if (Math.Abs(Math.Abs(cur.Value.Position.Y) - Math.Abs(node.Position.Y)) > area.Y)
                    {
                        break;
                    }
                    else if (Math.Abs(Math.Abs(cur.Value.Position.X) - Math.Abs(node.Position.X)) <= area.X)
                    {
                        if (Distance(node.Position, cur.Value.Position) <= area.Y)
                        {
                            if (!node.AoiInfo.MovesSet.Contains(cur.Value.Id)) node.AoiInfo.MovesSet.Add(cur.Value.Id);
                        }
                    }

                    cur = i == 0 ? cur.Next :cur.Previous;
                }
            }

            return node;
        }

        /// <summary>
        /// Get node
        /// </summary>
        /// <param name="id">Usually the character's ID or other identification ID</param>
        /// <returns></returns>
        public AoiNode GetNode(long id)
        {
            return _nodes.TryGetValue(id, out var node) ? node : null;
        }

        /// <summary>
        /// Exit AOI
        /// </summary>
        /// <param name="id">Usually the character's ID or other identification ID</param>
        /// <returns>List of coordinates that need to be notified</returns>
        public long[] LeaveNode(long id)
        {
            if (!_nodes.TryGetValue(id, out var node)) return null;
            
            _xLinks.Remove(node.Link.XNode);
            
            _yLinks.Remove(node.Link.YNode);
           
            _nodes.Remove(id);
            
            var aoiNodes = node.AoiInfo.MovesSet.ToArray();
            
            node.Dispose();
            
            return aoiNodes;
        }

        public double Distance(Vector2 a, Vector2 b)
        {
            return Math.Pow((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y), 0.5);
        }
    }
}
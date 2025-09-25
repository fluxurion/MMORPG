using System;
using System.Collections.Generic;

namespace AOI.Old
{
    public enum AoiNodeLinkedListType
    {
        XLink = 0,
        YLink = 1
    }

    public class AoiNodeLinkedList : LinkedList<AoiNode>
    {
        private readonly int _skipCount;

        private readonly AoiNodeLinkedListType _linkedListType;

        public AoiNodeLinkedList(int skip, AoiNodeLinkedListType linkedListType)
        {
            _skipCount = skip;

            _linkedListType = linkedListType;
        }

        public void Insert(AoiNode node)
        {
            if (_linkedListType == AoiNodeLinkedListType.XLink)
            {
                InsertX(node);
            }
            else
            {
                InsertY(node);
            }
        }

        #region Insert

        private void InsertX(AoiNode node)
        {
            if (First == null)
            {
                node.Link.XNode = AddFirst(Pool<LinkedListNode<AoiNode>>.Rent().Value = node);
            }
            else
            {
                var slowCursor = First;

                var skip = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(Count) / Convert.ToDouble(_skipCount)));

                if (Last.Value.Position.X > node.Position.X)
                {
                    for (var i = 0; i < _skipCount; i++)
                    {
                        // Move fast pointer

                        var fastCursor = FastCursor(skip, slowCursor?.Value);

                        // If the value of the fast pointer is less than the inserted value, assign the fast pointer to the slow pointer and use it as the current pointer.

                        if (fastCursor.Value.Position.X < node.Position.X)
                        {
                            slowCursor = fastCursor;

                            continue;
                        }

                        // The slow pointer moves to the fast pointer position

                        while (slowCursor != null)
                        {
                            if (slowCursor.Value.Position.X >= node.Position.X)
                            {
                                var xNode = Pool<LinkedListNode<AoiNode>>.Rent();
                                xNode.Value = node;
                                node.Link.XNode = xNode;

                                AddBefore(slowCursor,  node.Link.XNode);

                                return;
                            }

                            slowCursor = slowCursor.Next;
                        }
                    }
                }

                if (node.Link.XNode == null)
                {
                    node.Link.XNode = AddLast(Pool<LinkedListNode<AoiNode>>.Rent().Value = node);
                }
            }
        }

        private void InsertY(AoiNode node)
        {
            if (First == null)
            {
                node.Link.YNode = AddFirst(Pool<LinkedListNode<AoiNode>>.Rent().Value = node);
            }
            else
            {
                var slowCursor = First;

                var skip = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(Count) / Convert.ToDouble(_skipCount)));

                if (Last.Value.Position.Y > node.Position.Y)
                {
                    for (var i = 0; i < _skipCount; i++)
                    {
                        // Move fast pointer

                        var fastCursor = FastCursor(skip, slowCursor?.Value);

                        // If the value of the fast pointer is less than the inserted value, assign the fast pointer to the slow pointer and use it as the current pointer.

                        if (fastCursor.Value.Position.Y <= node.Position.Y)
                        {
                            slowCursor = fastCursor;

                            continue;
                        }

                        // The slow pointer moves to the fast pointer position

                        while (slowCursor != null)
                        {
                            if (slowCursor.Value.Position.Y >= node.Position.Y)
                            {
                                node.Link.YNode = Pool<LinkedListNode<AoiNode>>.Rent();
                                node.Link.YNode.Value = node;
                                AddBefore(slowCursor,  node.Link.YNode);
                                return;
                            }

                            slowCursor = slowCursor.Next;
                        }
                    }
                }

                if (node.Link.YNode == null)
                {
                    node.Link.YNode = AddLast(Pool<LinkedListNode<AoiNode>>.Rent().Value = node);
                }
            }
        }

        #endregion

        private LinkedListNode<AoiNode> FastCursor(int skip, AoiNode currentNode)
        {
            var skipLink = currentNode;

            switch (_linkedListType)
            {
                case AoiNodeLinkedListType.XLink:
                {
                    for (var i = 1; i <= skip; i++)
                    {
                        if (skipLink.Link.XNode.Next == null) break;

                        skipLink = skipLink.Link.XNode.Next.Value;
                    }
                
                    return skipLink.Link.XNode;
                }
                case AoiNodeLinkedListType.YLink:
                {
                    for (var i = 1; i <= skip; i++)
                    {
                        if (skipLink.Link.YNode.Next == null) break;

                        skipLink = skipLink.Link.YNode.Next.Value;
                    }
                
                    return skipLink.Link.YNode;
                }
                default:
                    return null;
            }
        }
    }
}
using System.Collections.Generic;

namespace BallSort.Core.Solver
{
    /// <summary>
    /// Binary min-heap of search nodes: lowest priority first, then lowest estimate (deepest, for
    /// equal A* priority), then newest. The runtime's .NET Standard 2.1 has no <c>PriorityQueue</c>.
    /// </summary>
    internal sealed class NodeHeap
    {
        private readonly List<SearchNode> _items = new List<SearchNode>();

        public int Count => _items.Count;

        public void Push(SearchNode node)
        {
            _items.Add(node);
            int child = _items.Count - 1;
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (!IsBefore(node, _items[parent]))
                {
                    break;
                }

                _items[child] = _items[parent];
                child = parent;
            }

            _items[child] = node;
        }

        public SearchNode Pop()
        {
            SearchNode top = _items[0];
            int lastIndex = _items.Count - 1;
            SearchNode last = _items[lastIndex];
            _items.RemoveAt(lastIndex);
            if (lastIndex == 0)
            {
                return top;
            }

            int index = 0;
            while (true)
            {
                int left = index * 2 + 1;
                if (left >= lastIndex)
                {
                    break;
                }

                int right = left + 1;
                int best = right < lastIndex && IsBefore(_items[right], _items[left]) ? right : left;
                if (!IsBefore(_items[best], last))
                {
                    break;
                }

                _items[index] = _items[best];
                index = best;
            }

            _items[index] = last;
            return top;
        }

        private static bool IsBefore(SearchNode a, SearchNode b)
        {
            if (a.Priority != b.Priority)
            {
                return a.Priority < b.Priority;
            }

            if (a.Estimate != b.Estimate)
            {
                return a.Estimate < b.Estimate;
            }

            return a.Sequence > b.Sequence;
        }
    }
}

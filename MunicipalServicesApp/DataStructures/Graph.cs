using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// One road between two vertices. Edges are undirected: the graph stores each road
    /// twice (once from each end) and both copies share the same Order number.
    /// </summary>
    public class GraphEdge<T>
    {
        public T From { get; private set; }
        public T To { get; private set; }

        /// <summary>Length of the road (for the service areas: kilometres).</summary>
        public int Weight { get; private set; }

        /// <summary>Order in which the road was added (0, 1, 2, ...). Used to break ties between equal weights.</summary>
        public int Order { get; private set; }

        public GraphEdge(T from, T to, int weight, int order)
        {
            From = from;
            To = to;
            Weight = weight;
            Order = order;
        }

        public override string ToString()
        {
            return From + " - " + To + " (" + Weight + ")";
        }
    }

    /// <summary>The outcome of a minimum-spanning-tree run.</summary>
    public class MstResult<T>
    {
        public string Algorithm { get; private set; }

        /// <summary>The chosen edges. For Prim, From is the vertex already in the tree and To is the one just added.</summary>
        public IReadOnlyList<GraphEdge<T>> Edges { get; private set; }

        public int TotalWeight { get; private set; }

        /// <summary>
        /// True if the edges connect every vertex. False for a graph in pieces (Kruskal then
        /// returns one tree per piece; Prim only covers the piece holding its start vertex).
        /// </summary>
        public bool SpansAllVertices { get; private set; }

        /// <summary>Comparisons made while ordering the edges (heap comparisons for Prim, sort comparisons for Kruskal).</summary>
        public int EdgeComparisons { get; private set; }

        public MstResult(string algorithm, List<GraphEdge<T>> edges, int totalWeight,
                         bool spansAllVertices, int edgeComparisons)
        {
            Algorithm = algorithm;
            Edges = edges.AsReadOnly();
            TotalWeight = totalWeight;
            SpansAllVertices = spansAllVertices;
            EdgeComparisons = edgeComparisons;
        }
    }

    /// <summary>
    /// An undirected, weighted graph stored as adjacency lists: every vertex keeps the
    /// list of roads that leave it. Memory is O(V + E), and walking the neighbours of
    /// a vertex costs only its own degree. (An adjacency matrix would need V x V cells,
    /// which is wasteful for a sparse road network.)
    ///
    /// Neighbours are visited in the order their roads were added, so every traversal
    /// gives the same result on every run.
    ///
    ///   BreadthFirst / DepthFirst : O(V + E)
    ///   PrimMst                   : O(E log V)  (binary heap)
    ///   KruskalMst                : O(E log E)  (sorting the edges; union-find is almost O(1))
    ///
    /// Methods that take a start vertex return an empty result if it is not in the graph.
    /// </summary>
    public class Graph<T>
    {
        private readonly IEqualityComparer<T> _comparer;
        private readonly Dictionary<T, int> _index;
        private readonly List<T> _vertices = new List<T>();
        private readonly List<List<GraphEdge<T>>> _adjacency = new List<List<GraphEdge<T>>>();
        private readonly List<GraphEdge<T>> _edges = new List<GraphEdge<T>>();

        public Graph(IEqualityComparer<T> comparer = null)
        {
            _comparer = comparer ?? EqualityComparer<T>.Default;
            _index = new Dictionary<T, int>(_comparer);
        }

        public int VertexCount
        {
            get { return _vertices.Count; }
        }

        public int EdgeCount
        {
            get { return _edges.Count; }
        }

        /// <summary>Vertices in the order they were first added.</summary>
        public IReadOnlyList<T> Vertices
        {
            get { return _vertices.AsReadOnly(); }
        }

        /// <summary>Every road once, in the order it was added.</summary>
        public IReadOnlyList<GraphEdge<T>> Edges
        {
            get { return _edges.AsReadOnly(); }
        }

        /// <summary>Sum of the weights of all roads.</summary>
        public int TotalWeight
        {
            get
            {
                int sum = 0;
                foreach (GraphEdge<T> edge in _edges)
                    sum += edge.Weight;
                return sum;
            }
        }

        // =====================================================================
        //  Building the graph
        // =====================================================================
        public bool Contains(T vertex)
        {
            return vertex != null && _index.ContainsKey(vertex);
        }

        /// <summary>Adds a vertex. Returns false if it is already there.</summary>
        public bool AddVertex(T vertex)
        {
            if (vertex == null) throw new ArgumentNullException("vertex");

            if (_index.ContainsKey(vertex))
                return false;

            EnsureVertex(vertex);
            return true;
        }

        /// <summary>
        /// Adds an undirected road, adding either vertex if it is new. Returns false (and
        /// changes nothing) for a road from a vertex to itself or a road that already exists.
        /// The weight must be greater than 0.
        /// </summary>
        public bool AddEdge(T a, T b, int weight)
        {
            if (a == null) throw new ArgumentNullException("a");
            if (b == null) throw new ArgumentNullException("b");
            if (weight <= 0)
                throw new ArgumentOutOfRangeException("weight", "An edge weight must be greater than 0.");

            if (_comparer.Equals(a, b))
                return false;

            int indexA = EnsureVertex(a);
            int indexB = EnsureVertex(b);

            foreach (GraphEdge<T> existing in _adjacency[indexA])
            {
                if (_comparer.Equals(existing.To, b))
                    return false;
            }

            int order = _edges.Count;
            var forward = new GraphEdge<T>(_vertices[indexA], _vertices[indexB], weight, order);
            var backward = new GraphEdge<T>(_vertices[indexB], _vertices[indexA], weight, order);

            _edges.Add(forward);
            _adjacency[indexA].Add(forward);
            _adjacency[indexB].Add(backward);
            return true;
        }

        private int EnsureVertex(T vertex)
        {
            int existing;
            if (_index.TryGetValue(vertex, out existing))
                return existing;

            int created = _vertices.Count;
            _vertices.Add(vertex);
            _adjacency.Add(new List<GraphEdge<T>>());
            _index[vertex] = created;
            return created;
        }

        // =====================================================================
        //  Looking things up
        // =====================================================================
        public bool TryGetWeight(T a, T b, out int weight)
        {
            weight = 0;

            int indexA;
            if (a == null || b == null || !_index.TryGetValue(a, out indexA))
                return false;

            foreach (GraphEdge<T> edge in _adjacency[indexA])
            {
                if (_comparer.Equals(edge.To, b))
                {
                    weight = edge.Weight;
                    return true;
                }
            }

            return false;
        }

        public bool HasEdge(T a, T b)
        {
            int ignored;
            return TryGetWeight(a, b, out ignored);
        }

        /// <summary>The roads leaving a vertex (From is always that vertex). Empty if the vertex is unknown.</summary>
        public IReadOnlyList<GraphEdge<T>> GetEdgesFrom(T vertex)
        {
            int index;
            if (vertex == null || !_index.TryGetValue(vertex, out index))
                return new List<GraphEdge<T>>().AsReadOnly();

            return _adjacency[index].AsReadOnly();
        }

        /// <summary>Number of roads that touch the vertex.</summary>
        public int Degree(T vertex)
        {
            return GetEdgesFrom(vertex).Count;
        }

        // =====================================================================
        //  Traversal
        // =====================================================================

        /// <summary>
        /// Breadth-first: the start vertex, then everything one road away, then everything
        /// two roads away, and so on. Uses a queue. Finds the fewest-roads distance.
        /// </summary>
        public List<T> BreadthFirst(T start)
        {
            var order = new List<T>();
            foreach (KeyValuePair<T, int> pair in BreadthFirstWithHops(start))
                order.Add(pair.Key);
            return order;
        }

        /// <summary>Breadth-first order, with how many roads each vertex is from the start (start = 0).</summary>
        public List<KeyValuePair<T, int>> BreadthFirstWithHops(T start)
        {
            var result = new List<KeyValuePair<T, int>>();

            int startIndex;
            if (start == null || !_index.TryGetValue(start, out startIndex))
                return result;

            var seen = new bool[_vertices.Count];
            var hops = new int[_vertices.Count];
            var queue = new Queue<int>();

            seen[startIndex] = true;
            queue.Enqueue(startIndex);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                result.Add(new KeyValuePair<T, int>(_vertices[current], hops[current]));

                foreach (GraphEdge<T> edge in _adjacency[current])
                {
                    int next = _index[edge.To];
                    if (!seen[next])
                    {
                        seen[next] = true; // marked when queued, so nothing is queued twice
                        hops[next] = hops[current] + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Depth-first: follow one road as far as possible, then back up and try the next.
        /// Uses an explicit stack, so a long chain can never overflow the call stack.
        /// Neighbours are pushed in reverse so the first road added is explored first.
        /// </summary>
        public List<T> DepthFirst(T start)
        {
            var order = new List<T>();

            int startIndex;
            if (start == null || !_index.TryGetValue(start, out startIndex))
                return order;

            var visited = new bool[_vertices.Count];
            var stack = new Stack<int>();
            stack.Push(startIndex);

            while (stack.Count > 0)
            {
                int current = stack.Pop();
                if (visited[current])
                    continue; // reached by another route while it waited on the stack

                visited[current] = true;
                order.Add(_vertices[current]);

                List<GraphEdge<T>> edges = _adjacency[current];
                for (int i = edges.Count - 1; i >= 0; i--)
                {
                    int next = _index[edges[i].To];
                    if (!visited[next])
                        stack.Push(next);
                }
            }

            return order;
        }

        /// <summary>The separate pieces of the graph. Each piece lists its vertices in breadth-first order.</summary>
        public List<List<T>> ConnectedComponents()
        {
            var components = new List<List<T>>();
            var seen = new bool[_vertices.Count];

            for (int start = 0; start < _vertices.Count; start++)
            {
                if (seen[start])
                    continue;

                var component = new List<T>();
                var queue = new Queue<int>();
                seen[start] = true;
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    component.Add(_vertices[current]);

                    foreach (GraphEdge<T> edge in _adjacency[current])
                    {
                        int next = _index[edge.To];
                        if (!seen[next])
                        {
                            seen[next] = true;
                            queue.Enqueue(next);
                        }
                    }
                }

                components.Add(component);
            }

            return components;
        }

        /// <summary>True if every vertex can be reached from every other (an empty graph counts as connected).</summary>
        public bool IsConnected()
        {
            return _vertices.Count == 0 || ConnectedComponents().Count == 1;
        }

        // =====================================================================
        //  Minimum spanning tree
        // =====================================================================

        /// <summary>
        /// Prim's algorithm: start with one vertex and keep adding the cheapest road that
        /// leads from the tree to a vertex not yet in it. A min-heap of candidate roads
        /// gives the cheapest one in O(log n). Roads whose far end has joined the tree in
        /// the meantime are skipped when they come off the heap.
        /// </summary>
        public MstResult<T> PrimMst(T start)
        {
            var chosen = new List<GraphEdge<T>>();

            int startIndex;
            if (start == null || !_index.TryGetValue(start, out startIndex))
                return new MstResult<T>("Prim", chosen, 0, _vertices.Count == 0, 0);

            var visited = new bool[_vertices.Count];
            var heap = new BinaryHeap<GraphEdge<T>>(new Comparison<GraphEdge<T>>(CompareEdges));
            int total = 0;
            int visitedCount = 1;

            visited[startIndex] = true;
            PushUnvisitedEdges(startIndex, visited, heap);

            while (heap.Count > 0 && visitedCount < _vertices.Count)
            {
                GraphEdge<T> edge = heap.Dequeue();
                int to = _index[edge.To];
                if (visited[to])
                    continue;

                visited[to] = true;
                visitedCount++;
                chosen.Add(edge);
                total += edge.Weight;
                PushUnvisitedEdges(to, visited, heap);
            }

            return new MstResult<T>("Prim", chosen, total,
                visitedCount == _vertices.Count, heap.TotalComparisons);
        }

        private void PushUnvisitedEdges(int vertex, bool[] visited, BinaryHeap<GraphEdge<T>> heap)
        {
            foreach (GraphEdge<T> edge in _adjacency[vertex])
            {
                if (!visited[_index[edge.To]])
                    heap.Enqueue(edge);
            }
        }

        /// <summary>
        /// Kruskal's algorithm: sort every road from cheapest to dearest and accept a road
        /// only if its two ends are not already connected (otherwise it would close a loop).
        /// The union-find (DisjointSet) answers that question. If the graph is in pieces
        /// the result is a spanning forest and SpansAllVertices is false.
        /// </summary>
        public MstResult<T> KruskalMst()
        {
            int comparisons = 0;
            var sorted = new List<GraphEdge<T>>(_edges);
            sorted.Sort((a, b) =>
            {
                comparisons++;
                return CompareEdges(a, b);
            });

            var groups = new DisjointSet(_vertices.Count);
            var chosen = new List<GraphEdge<T>>();
            int total = 0;

            foreach (GraphEdge<T> edge in sorted)
            {
                if (groups.Union(_index[edge.From], _index[edge.To]))
                {
                    chosen.Add(edge);
                    total += edge.Weight;

                    if (chosen.Count == _vertices.Count - 1)
                        break; // a spanning tree of n vertices has n - 1 edges: nothing more to find
                }
            }

            bool spans = _vertices.Count == 0 || chosen.Count == _vertices.Count - 1;
            return new MstResult<T>("Kruskal", chosen, total, spans, comparisons);
        }

        /// <summary>Cheaper road first; equal weights are ordered by when the road was added.</summary>
        private static int CompareEdges(GraphEdge<T> a, GraphEdge<T> b)
        {
            int byWeight = a.Weight.CompareTo(b.Weight);
            return byWeight != 0 ? byWeight : a.Order.CompareTo(b.Order);
        }
    }
}
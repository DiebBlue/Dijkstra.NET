using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dijkstra.NET.Extensions;
using Dijkstra.NET.PageRank;
using Dijkstra.NET.ShortestPath;

namespace Dijkstra.NET.Graph.Simple
{
    /// <summary>
    /// Simple graph for nodes processing
    /// </summary>
    public class Graph : IDijkstraGraph, IPageRankGraph
    {
        private readonly Dictionary<uint, HashSet<ReadonlyEdge>> _nodes = new Dictionary<uint, HashSet<ReadonlyEdge>>();
        private readonly Dictionary<uint, HashSet<uint>> _nodesParent = new Dictionary<uint, HashSet<uint>>();

        /// <summary>
        /// Connect node with node
        /// </summary>
        /// <param name="graph">Graph</param>
        /// <param name="node">Key of node</param>
        /// <returns>Temporary edge</returns>
        public static EdgeTemp operator >>(Graph graph, int node)
        {
            return new EdgeTemp(graph, (uint)node);
        }

        public Graph(int nodeCapacity = 0)
        {
            if (nodeCapacity > 0)
            {
                _nodes = new Dictionary<uint, HashSet<ReadonlyEdge>>(nodeCapacity);
                _nodesParent = new Dictionary<uint, HashSet<uint>>(nodeCapacity);
            }
            else
            {
                _nodes = new Dictionary<uint, HashSet<ReadonlyEdge>>(nodeCapacity);
                _nodesParent = new Dictionary<uint, HashSet<uint>>(nodeCapacity);
            }
        }

        /// <summary>
        /// Add nodes to graph
        /// </summary>
        /// <param name="graph">Graph</param>
        /// <param name="numberOfNodes">Number of nodes</param>
        /// <returns></returns>
        public static Graph operator +(Graph graph, int numberOfNodes)
        {
            Enumerable
                .Range(0, numberOfNodes)
                .Each(_ => graph.AddNode());

            return graph;
        }

        /// <summary>
        /// Ensures that the dictionary can hold up to a specified number of entries without any further expansion of its backing storage.
        /// </summary>
        /// <param name="expectedNodes">expected maximum nodecount</param>
        public void EnsureCapacity(int expectedNodes)
        {
            if (expectedNodes <= _nodes.Count)
                return;
            // check if it is Net6_0 or higher
#if NET6_0_OR_GREATER
            _nodes.EnsureCapacity(expectedNodes);
            _nodesParent.EnsureCapacity(expectedNodes);
#endif
        }

        /// <summary>
        /// Add node to graph
        /// </summary>
        /// <param name="edgeCapacity">expected edges for the node (for speed optimization)</param>
        /// <returns>key</returns>
        public uint AddNode(int edgeCapacity = 0)
        {
            uint key = (uint)(_nodes.Count + 1);
#if NET6_0_OR_GREATER
            _nodes.Add(key, new HashSet<ReadonlyEdge>(edgeCapacity));
            _nodesParent.Add(key, new HashSet<uint>(edgeCapacity));
#else
            _nodes.Add(key, new HashSet<ReadonlyEdge>());
            _nodesParent.Add(key, new HashSet<uint>());
#endif
            return key;
        }

        /// <summary>
        ///  Remove node from graph and all edges connected to it
        /// </summary>
        /// <param name="key">the key</param>
        public void RemoveNode(uint key)
        {
            // remove this node from all parent node connections

            // find the node with the key
            if (_nodes.TryGetValue(key, out var node))
            {
                // iterate over all edges from node and remove the node from the parent list
                foreach (var edge in node)
                {
                    if (_nodesParent.TryGetValue(edge.Key, out var parentnode))
                        parentnode.Remove(key);
                }
                // remove the node from the node list
                _nodes.Remove(key);
            }

            // remove the node as a parent node from all nodes

            // find the node with the key in the parent list
            if (_nodesParent.TryGetValue(key, out var parent))
            {
                // iterate over all parent nodes and remove the node from the edge list
                foreach (var parentKey in parent)
                {
                    if (_nodes.TryGetValue(parentKey, out var edges))
                    {
                        edges.RemoveWhere(e => e.Key == key);
                    }
                }
                // remove the node from the parent list
                _nodesParent.Remove(key);
            }
        }

        /// <summary>
        /// Connect node from to node to with cost
        /// (from)-[cost]->(to)
        /// </summary>
        /// <param name="from">Node from</param>
        /// <param name="to">Node to</param>
        /// <param name="cost">Cost of connection</param>
        /// <returns>True if two nodes exist</returns>
        public bool Connect(uint from, uint to, int cost)
        {
            /*
            if (!_nodes.ContainsKey(from) || !_nodes.ContainsKey(to))
                return false;*/

            if (_nodesParent.TryGetValue(to, out var parentnode) && _nodes.TryGetValue(from, out var node))
            {
                parentnode.Add(from);
                node.Add(new ReadonlyEdge(to, cost));
                return true;
            }
            else
                return false;

            //_nodesParent[to].Add(from);
            //_nodes[from].Add(new ReadonlyEdge(to, cost));

            //return true;
        }

        /// <summary>
        /// Connect node from to node to
        /// (from)-[]->(to)
        /// </summary>
        /// <param name="from">Node from</param>
        /// <param name="to">Node to</param>
        /// <returns>True if two nodes exist</returns>
        public bool Connect(uint from, uint to) => Connect(from, to, -1);

        /// <summary>
        /// Get nodes with cost
        /// </summary>
        /// <param name="node"></param>
        public Action<Edge> this[uint node] => e => _nodes[node].Each(n => e(n.Key, n.Cost));

        public IEnumerator<uint> GetEnumerator()
        {
            foreach (var node in _nodes)
            {
                yield return node.Key;
            }
        }

        public override string ToString()
        {
            return $"Simple::Graph({NodesCount})";
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int NodesCount => _nodes.Count;

        public int EdgesCount(uint node) => _nodes[node].Count;

        public IEnumerable<uint> Parents(uint node) => _nodesParent[node];
    }
}
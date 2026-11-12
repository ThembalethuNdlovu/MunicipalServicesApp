using System;
using System.Collections.Generic;
using System.Linq;
using MunicipalServicesApp.Services;

namespace MunicipalServicesApp.DataStructures
{
    public static partial class StructureSelfTests
    {
        private static void TestGraph(Report r)
        {
            TestDisjointSet(r);
            TestGraphStructure(r);
            TestGraphTraversal(r);
            TestSpanningTrees(r);
            TestDisconnectedGraph(r);
            TestServiceAreaCatalog(r);
        }

        // =====================================================================
        private static void TestDisjointSet(Report r)
        {
            r.Heading("Disjoint set (union-find, used by Kruskal)");

            var sets = new DisjointSet(6);
            r.Check("6 separate groups at the start, nothing connected",
                sets.SetCount == 6 && !sets.Connected(0, 1));

            bool first = sets.Union(0, 1);
            bool second = sets.Union(2, 3);
            bool third = sets.Union(1, 3);
            r.Check("Joining different groups succeeds and merges them (0-1, 2-3, then 1-3)",
                first && second && third && sets.SetCount == 3 &&
                sets.Connected(0, 2) && !sets.Connected(0, 4));

            r.Check("Joining two items already in the same group returns false and changes nothing",
                !sets.Union(0, 3) && sets.SetCount == 3);
        }

        private static void TestGraphStructure(Report r)
        {
            r.Heading("Graph: structure");

            var empty = new Graph<string>();
            r.Check("Empty graph: no vertices or edges, counts as connected, traversals and spanning trees are empty",
                empty.VertexCount == 0 && empty.EdgeCount == 0 && empty.IsConnected() &&
                empty.BreadthFirst("A").Count == 0 && empty.DepthFirst("A").Count == 0 &&
                empty.KruskalMst().Edges.Count == 0 && empty.PrimMst("A").Edges.Count == 0);

            Graph<string> g = BuildTestGraph();
            r.Check("Test graph has 5 vertices and 5 edges; vertices keep the order they were first used",
                g.VertexCount == 5 && g.EdgeCount == 5 && string.Join(",", g.Vertices) == "A,B,C,D,E");

            int weightAb, weightBa;
            bool foundAb = g.TryGetWeight("A", "B", out weightAb);
            bool foundBa = g.TryGetWeight("B", "A", out weightBa);
            r.Check("Edges are undirected: A-B is found from both ends with weight 4; A-E does not exist",
                foundAb && foundBa && weightAb == 4 && weightBa == 4 &&
                g.HasEdge("B", "A") && !g.HasEdge("A", "E"));

            r.Check("A duplicate edge (even reversed) and a self-loop are rejected without changing anything",
                !g.AddEdge("B", "A", 9) && !g.AddEdge("E", "E", 1) && !g.AddEdge("Z", "Z", 1) &&
                g.EdgeCount == 5 && !g.Contains("Z") && g.TotalWeight == 15);

            r.Check("Invalid input is rejected: weight 0 and a null vertex both throw",
                GraphThrows<ArgumentOutOfRangeException>(() => g.AddEdge("A", "E", 0)) &&
                GraphThrows<ArgumentNullException>(() => g.AddEdge(null, "A", 1)));

            r.Check("D touches B, C and E (in the order its roads were added); an unknown vertex has no roads",
                g.Degree("D") == 3 && g.Degree("E") == 1 && g.Degree("Z") == 0 &&
                string.Join(",", g.GetEdgesFrom("D").Select(edge => edge.To)) == "B,C,E");
        }

        private static void TestGraphTraversal(Report r)
        {
            r.Heading("Graph: breadth-first and depth-first traversal");

            Graph<string> g = BuildTestGraph();

            r.Check("Breadth-first from A goes level by level: A,B,C,D,E",
                string.Join(",", g.BreadthFirst("A")) == "A,B,C,D,E");
            r.Check("Depth-first from A goes as deep as it can before backing up: A,B,D,C,E",
                string.Join(",", g.DepthFirst("A")) == "A,B,D,C,E");
            r.Check("Hops from A: A:0,B:1,C:1,D:2,E:3",
                string.Join(",", g.BreadthFirstWithHops("A").Select(p => p.Key + ":" + p.Value)) ==
                "A:0,B:1,C:1,D:2,E:3");
            r.Check("Breadth-first from E gives E,D,B,C,A (the start vertex changes the order)",
                string.Join(",", g.BreadthFirst("E")) == "E,D,B,C,A");
            r.Check("An unknown or null start vertex gives an empty traversal",
                g.BreadthFirst("Z").Count == 0 && g.DepthFirst("Z").Count == 0 &&
                g.BreadthFirstWithHops(null).Count == 0);
            r.Check("The test graph is a single connected component",
                g.IsConnected() && g.ConnectedComponents().Count == 1);
        }

        private static void TestSpanningTrees(Report r)
        {
            r.Heading("Minimum spanning tree: Kruskal and Prim");

            Graph<string> g = BuildTestGraph();

            MstResult<string> kruskal = g.KruskalMst();
            r.Check("Kruskal (cheapest road first, skip any that close a loop): A-B, A-C, C-D, D-E, total 10",
                EdgeSet(kruskal.Edges) == "A-B,A-C,C-D,D-E" && kruskal.TotalWeight == 10 &&
                kruskal.SpansAllVertices);

            MstResult<string> primFromA = g.PrimMst("A");
            r.Check("Prim from A (grow outwards using a heap) finds the same edges and total",
                EdgeSet(primFromA.Edges) == "A-B,A-C,C-D,D-E" && primFromA.TotalWeight == 10 &&
                primFromA.SpansAllVertices);

            MstResult<string> primFromE = g.PrimMst("E");
            r.Check("Prim from E gives the same tree: the result does not depend on the start vertex",
                EdgeSet(primFromE.Edges) == "A-B,A-C,C-D,D-E" && primFromE.TotalWeight == 10);

            r.Check("The tree has V - 1 = 4 edges and is cheaper than the whole graph (10 < 15); B-D (5) is dropped",
                kruskal.Edges.Count == 4 && kruskal.TotalWeight < g.TotalWeight &&
                !EdgeSet(kruskal.Edges).Contains("B-D"));

            var triangle = new Graph<string>();
            triangle.AddEdge("A", "B", 5);
            triangle.AddEdge("B", "C", 5);
            triangle.AddEdge("A", "C", 5);
            MstResult<string> triangleKruskal = triangle.KruskalMst();
            MstResult<string> trianglePrim = triangle.PrimMst("A");
            r.Check("Equal weights are broken by the order the roads were added, so both agree: A-B, B-C, total 10",
                EdgeSet(triangleKruskal.Edges) == "A-B,B-C" && triangleKruskal.TotalWeight == 10 &&
                EdgeSet(trianglePrim.Edges) == "A-B,B-C" && trianglePrim.TotalWeight == 10);

            var single = new Graph<string>();
            single.AddVertex("Only");
            MstResult<string> singleKruskal = single.KruskalMst();
            MstResult<string> singlePrim = single.PrimMst("Only");
            r.Check("A single vertex: both algorithms return no edges, total 0, and span the graph",
                singleKruskal.Edges.Count == 0 && singleKruskal.TotalWeight == 0 && singleKruskal.SpansAllVertices &&
                singlePrim.Edges.Count == 0 && singlePrim.TotalWeight == 0 && singlePrim.SpansAllVertices);

            r.Check("Both algorithms report how many edge comparisons they made",
                kruskal.EdgeComparisons > 0 && primFromA.EdgeComparisons > 0);
            r.Note(string.Format("Test graph: Kruskal {0} comparisons, Prim {1} comparisons (heap)",
                kruskal.EdgeComparisons, primFromA.EdgeComparisons));
        }

        private static void TestDisconnectedGraph(Report r)
        {
            r.Heading("Graph in pieces (a vertex with no roads)");

            Graph<string> g = BuildTestGraph();
            bool added = g.AddVertex("F");
            bool addedAgain = g.AddVertex("F");
            List<List<string>> pieces = g.ConnectedComponents();

            r.Check("Adding F: true the first time, false the second; graph is now in 2 pieces: A,B,C,D,E and F",
                added && !addedAgain && !g.IsConnected() && pieces.Count == 2 &&
                string.Join(",", pieces[0]) == "A,B,C,D,E" && string.Join(",", pieces[1]) == "F");

            r.Check("Traversals stay inside their piece: from A they skip F; from F they find only F",
                g.BreadthFirst("A").Count == 5 && g.DepthFirst("A").Count == 5 &&
                string.Join(",", g.BreadthFirst("F")) == "F" && string.Join(",", g.DepthFirst("F")) == "F");

            MstResult<string> primPartial = g.PrimMst("A");
            MstResult<string> primFromF = g.PrimMst("F");
            MstResult<string> primUnknown = g.PrimMst("Z");
            r.Check("Prim only covers its start vertex's piece: 4 edges, total 10, not spanning; from F or an unknown vertex it finds nothing",
                primPartial.Edges.Count == 4 && primPartial.TotalWeight == 10 && !primPartial.SpansAllVertices &&
                primFromF.Edges.Count == 0 && !primFromF.SpansAllVertices &&
                primUnknown.Edges.Count == 0 && !primUnknown.SpansAllVertices);

            MstResult<string> forest = g.KruskalMst();
            r.Check("Kruskal returns a spanning forest: the same 4 edges, total 10, flagged as not spanning everything",
                EdgeSet(forest.Edges) == "A-B,A-C,C-D,D-E" && forest.TotalWeight == 10 &&
                !forest.SpansAllVertices);
        }

        private static void TestServiceAreaCatalog(Report r)
        {
            r.Heading("Service-area graph and location matching");

            Graph<string> areas = ServiceAreaCatalog.BuildGraph();
            r.Check("Catalogue: 10 areas and 17 roads, all connected; area names are found whatever their letter case",
                areas.VertexCount == 10 && areas.EdgeCount == 17 && areas.IsConnected() &&
                areas.Contains("sandton") && ServiceAreaCatalog.Areas.Count == 10);

            Dictionary<string, int> hops = areas.BreadthFirstWithHops("Sandton")
                                                .ToDictionary(p => p.Key, p => p.Value);
            string[] names =
            {
                "Sandton", "Alexandra", "Braamfontein", "Randburg", "Fourways",
                "Midrand", "Soweto", "Diepsloot", "Roodepoort", "Orange Farm"
            };
            r.Check("Hops from Sandton: 5 areas are 1 road away, Soweto/Diepsloot/Roodepoort 2, Orange Farm 3",
                hops.Count == 10 &&
                string.Join(",", names.Select(n => n + ":" + hops[n])) ==
                "Sandton:0,Alexandra:1,Braamfontein:1,Randburg:1,Fourways:1,Midrand:1," +
                "Soweto:2,Diepsloot:2,Roodepoort:2,Orange Farm:3");

            List<string> depthFirst = areas.DepthFirst("Sandton");
            r.Check("Depth-first from Sandton visits all 10 areas exactly once",
                depthFirst.Count == 10 && depthFirst.Distinct().Count() == 10 && depthFirst[0] == "Sandton");

            MstResult<string> kruskal = areas.KruskalMst();
            MstResult<string> primFromSoweto = areas.PrimMst("Soweto");
            MstResult<string> primFromFarm = areas.PrimMst("Orange Farm");
            string kruskalSet = EdgeSet(kruskal.Edges);
            r.Check("Kruskal and Prim (from two different starts) all find a 118 km network of 9 roads",
                kruskal.TotalWeight == 118 && kruskal.Edges.Count == 9 && kruskal.SpansAllVertices &&
                primFromSoweto.TotalWeight == 118 && primFromFarm.TotalWeight == 118 &&
                EdgeSet(primFromSoweto.Edges) == kruskalSet && EdgeSet(primFromFarm.Edges) == kruskalSet);

            r.Check("The 9 chosen roads are the expected ones; the 13 km Alexandra-Braamfontein road is left out",
                kruskalSet == "Alexandra-Sandton,Braamfontein-Sandton,Braamfontein-Soweto,Diepsloot-Fourways," +
                              "Fourways-Midrand,Fourways-Randburg,Orange Farm-Soweto,Randburg-Roodepoort,Randburg-Sandton");

            r.Note(string.Format("All {0} roads add up to {1} km; the spanning tree needs only {2} km ({3} km less).",
                areas.EdgeCount, areas.TotalWeight, kruskal.TotalWeight, areas.TotalWeight - kruskal.TotalWeight));
            r.Note("Spanning-tree roads: " +
                   string.Join("; ", kruskal.Edges.Select(edge => edge.From + " - " + edge.To + " " + edge.Weight + " km")));
            r.Note("Breadth-first from Sandton: " + string.Join(" > ", areas.BreadthFirst("Sandton")));
            r.Note("Depth-first from Sandton:   " + string.Join(" > ", depthFirst));

            r.Check("Locations: the text after the last comma wins (Alexandra Road, Sandton is in Sandton)",
                ServiceAreaCatalog.FindArea("Vilakazi Street, Soweto") == "Soweto" &&
                ServiceAreaCatalog.FindArea("Alexandra Road, Sandton") == "Sandton" &&
                ServiceAreaCatalog.FindArea("Old Pretoria Road, Midrand") == "Midrand");
            r.Check("Locations: letter case and extra spaces do not matter; the catalogue spelling is returned",
                ServiceAreaCatalog.FindArea("  main road,   RANDBURG ") == "Randburg" &&
                ServiceAreaCatalog.FindArea("extension 4, orange farm") == "Orange Farm");
            r.Check("Locations: an area name with no comma is still found",
                ServiceAreaCatalog.FindArea("Alexandra") == "Alexandra" &&
                ServiceAreaCatalog.FindArea("near the Fourways mall") == "Fourways");
            r.Check("Locations: null, blank or an unknown place gives Unassigned",
                ServiceAreaCatalog.FindArea(null) == ServiceAreaCatalog.Unassigned &&
                ServiceAreaCatalog.FindArea("") == ServiceAreaCatalog.Unassigned &&
                ServiceAreaCatalog.FindArea("   ") == ServiceAreaCatalog.Unassigned &&
                ServiceAreaCatalog.FindArea("12 Nowhere Street, Pretoria") == ServiceAreaCatalog.Unassigned);
            r.Check("IsKnownArea ignores letter case and does not accept Unassigned or null",
                ServiceAreaCatalog.IsKnownArea("sandton") && ServiceAreaCatalog.IsKnownArea("Orange Farm") &&
                !ServiceAreaCatalog.IsKnownArea("Unassigned") && !ServiceAreaCatalog.IsKnownArea(null));
        }

        // =====================================================================
        /// <summary>
        /// A, B, C, D, E with a loop (A-B-D-C): A-B 4, A-C 2, B-D 5, C-D 1, D-E 3.
        /// All weights differ, so its minimum spanning tree is unique.
        /// </summary>
        private static Graph<string> BuildTestGraph()
        {
            var graph = new Graph<string>();
            graph.AddEdge("A", "B", 4);
            graph.AddEdge("A", "C", 2);
            graph.AddEdge("B", "D", 5);
            graph.AddEdge("C", "D", 1);
            graph.AddEdge("D", "E", 3);
            return graph;
        }

        /// <summary>The edges as a sorted, direction-free text such as "A-B,C-D", so results can be compared.</summary>
        private static string EdgeSet(IEnumerable<GraphEdge<string>> edges)
        {
            var parts = new List<string>();
            foreach (GraphEdge<string> edge in edges)
            {
                bool inOrder = string.CompareOrdinal(edge.From, edge.To) <= 0;
                parts.Add(inOrder ? edge.From + "-" + edge.To : edge.To + "-" + edge.From);
            }

            parts.Sort(StringComparer.Ordinal);
            return string.Join(",", parts);
        }

        private static bool GraphThrows<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return true;
            }
            return false;
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// Built-in checks for the data structures. RunAll() returns a readable report
    /// with PASS / FAIL for every check. Later steps add their own checks here.
    /// </summary>
    public static class StructureSelfTests
    {
        private class Report
        {
            public readonly StringBuilder Text = new StringBuilder();
            public int Passed;
            public int Failed;

            public void Check(string name, bool ok)
            {
                if (ok) Passed++; else Failed++;
                Text.AppendLine((ok ? "PASS  " : "FAIL  ") + name);
            }

            public void Note(string text)
            {
                Text.AppendLine("      " + text);
            }

            public void Heading(string text)
            {
                Text.AppendLine();
                Text.AppendLine("--- " + text + " ---");
            }
        }

        public static string RunAll()
        {
            var report = new Report();

            TestEmptyTree(report);
            TestBinarySearchTree(report);
            TestDegenerateBst(report);
            TestGeneralTree(report);

            var summary = new StringBuilder();
            summary.AppendLine(string.Format("Result: {0} passed, {1} failed", report.Passed, report.Failed));
            summary.Append(report.Text);
            return summary.ToString();
        }

        // =====================================================================
        private static void TestEmptyTree(Report r)
        {
            r.Heading("Empty tree");
            var tree = new BinarySearchTree<int, string>();

            int key;
            string value;
            r.Check("Empty tree has count 0 and height 0", tree.Count == 0 && tree.Height == 0);
            r.Check("Empty tree: traversals yield nothing",
                !tree.InOrder().Any() && !tree.PreOrder().Any() &&
                !tree.PostOrder().Any() && !tree.LevelOrder().Any());
            r.Check("Empty tree: min/max/search find nothing",
                !tree.TryGetMin(out key, out value) &&
                !tree.TryGetMax(out key, out value) &&
                !tree.TryGetValue(1, out value));
        }

        private static void TestBinarySearchTree(Report r)
        {
            r.Heading("Binary search tree");
            var bst = new BinarySearchTree<int, string>();
            foreach (int key in new[] { 50, 30, 70, 20, 40, 60, 80 })
                bst.Insert(key, "v" + key);

            r.Check("Count is 7", bst.Count == 7);
            r.Check("Height is 3", bst.Height == 3);
            r.Check("In-order is sorted", Keys(bst.InOrder()) == "20,30,40,50,60,70,80");
            r.Check("Pre-order", Keys(bst.PreOrder()) == "50,30,20,40,70,60,80");
            r.Check("Post-order", Keys(bst.PostOrder()) == "20,40,30,60,80,70,50");
            r.Check("Level-order", Keys(bst.LevelOrder()) == "50,30,70,20,40,60,80");

            r.Check("Duplicate key is rejected", !bst.Insert(40, "other") && bst.Count == 7);

            string found;
            r.Check("Search finds 60 and returns its value", bst.TryGetValue(60, out found) && found == "v60");
            r.Check("Search for 65 (missing) fails", !bst.TryGetValue(65, out found));

            int minKey, maxKey;
            string minValue, maxValue;
            r.Check("Min is 20 and max is 80",
                bst.TryGetMin(out minKey, out minValue) && minKey == 20 &&
                bst.TryGetMax(out maxKey, out maxValue) && maxKey == 80);

            r.Check("Range 35..65 gives 40,50,60", Keys(bst.GetRange(35, 65)) == "40,50,60");
            r.Check("Range 50..50 gives 50", Keys(bst.GetRange(50, 50)) == "50");
            r.Check("Range with low > high gives nothing", !bst.GetRange(80, 20).Any());

            List<string> diagram = bst.ToDiagramLines();
            r.Check("Diagram has one line per node, largest key first",
                diagram.Count == 7 && diagram[0].Trim() == "80");

            r.Note("Diagram (root on the left, larger keys on top):");
            foreach (string line in diagram)
                r.Note(line);
        }

        private static void TestDegenerateBst(Report r)
        {
            r.Heading("BST weakness: sorted insertion order");

            // Sequence numbers are 1, 2, 3, ... so real data arrives already sorted
            var sequential = new BinarySearchTree<int, int>();
            for (int i = 1; i <= 200; i++)
                sequential.Insert(i, i);

            int value;
            sequential.TryGetValue(200, out value);
            int sequentialHeight = sequential.Height;
            int sequentialComparisons = sequential.LastOperationComparisons;

            // The same 200 keys, inserted median-first, give a well balanced tree
            var balanced = new BinarySearchTree<int, int>();
            InsertMedianFirst(balanced, 1, 200);
            balanced.TryGetValue(200, out value);
            int balancedHeight = balanced.Height;
            int balancedComparisons = balanced.LastOperationComparisons;

            r.Check("Sorted insertion: height equals the node count (200)", sequentialHeight == 200);
            r.Check("Sorted insertion: finding key 200 takes 200 comparisons", sequentialComparisons == 200);
            r.Check("Median-first insertion: height is at most 8", balancedHeight <= 8);
            r.Note(string.Format("Sorted order : height {0}, {1} comparisons to find key 200",
                sequentialHeight, sequentialComparisons));
            r.Note(string.Format("Balanced order: height {0}, {1} comparisons to find key 200",
                balancedHeight, balancedComparisons));
        }

        private static void TestGeneralTree(Report r)
        {
            r.Heading("General tree");
            var tree = new GeneralTree<string>("Municipality");
            GeneralTreeNode<string> roads = tree.AddChild(tree.Root, "Roads");
            GeneralTreeNode<string> water = tree.AddChild(tree.Root, "Water");
            tree.AddChild(roads, "Potholes");
            tree.AddChild(roads, "Signage");
            tree.AddChild(water, "Leaks");

            r.Check("Count is 6 and height is 3", tree.Count == 6 && tree.Height == 3);
            r.Check("Pre-order (depth-first)",
                string.Join(",", tree.PreOrder().Select(n => n.Value)) ==
                "Municipality,Roads,Potholes,Signage,Water,Leaks");
            r.Check("Level-order (breadth-first)",
                string.Join(",", tree.LevelOrder().Select(n => n.Value)) ==
                "Municipality,Roads,Water,Potholes,Signage,Leaks");

            GeneralTreeNode<string> leaks = tree.Find(v => v == "Leaks");
            r.Check("Find 'Leaks': depth 2, parent is 'Water', is a leaf",
                leaks != null && leaks.Depth == 2 && leaks.Parent == water && leaks.IsLeaf);
            r.Check("Find a missing value returns null", tree.Find(v => v == "Parks") == null);
        }

        // =====================================================================
        private static string Keys(IEnumerable<KeyValuePair<int, string>> entries)
        {
            return string.Join(",", entries.Select(e => e.Key));
        }

        private static void InsertMedianFirst(BinarySearchTree<int, int> tree, int low, int high)
        {
            if (low > high) return;

            int middle = low + (high - low) / 2;
            tree.Insert(middle, middle);
            InsertMedianFirst(tree, low, middle - 1);
            InsertMedianFirst(tree, middle + 1, high);
        }
    }
}
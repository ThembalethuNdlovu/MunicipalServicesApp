using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MunicipalServicesApp.Services;

namespace MunicipalServicesApp.DataStructures
{
    /// <summary>
    /// Built-in checks for the data structures. RunAll() returns a readable report
    /// with PASS / FAIL for every check. Later steps add their own checks here.
    /// </summary>
    public static partial class StructureSelfTests
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
            TestAvlTree(report);
            TestRedBlackTree(report);
            TestBinaryHeap(report);
            TestGraph(report);
            TestServiceRequestService(report);

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

        private static void TestAvlTree(Report r)
        {
            r.Heading("AVL tree: rotations");

            var empty = new AvlTree<int, string>();
            r.Check("Empty AVL tree is valid with height 0",
                empty.Validate() && empty.Height == 0 && empty.Count == 0);

            CheckRotation(r, "Left-left case (insert 30,20,10): one right rotation",
                new[] { 30, 20, 10 }, 1);
            CheckRotation(r, "Right-right case (insert 10,20,30): one left rotation",
                new[] { 10, 20, 30 }, 1);
            CheckRotation(r, "Left-right case (insert 30,10,20): double rotation",
                new[] { 30, 10, 20 }, 2);
            CheckRotation(r, "Right-left case (insert 10,30,20): double rotation",
                new[] { 10, 30, 20 }, 2);

            var seven = new AvlTree<int, string>();
            for (int i = 1; i <= 7; i++)
                seven.Insert(i, "v" + i);

            r.Check("Inserting 1..7 in order gives a perfect tree of height 3",
                Keys(seven.PreOrder()) == "4,2,1,3,6,5,7" && seven.Height == 3 && seven.Validate());
            r.Note("Diagram after inserting 1..7 in order (key and node height):");
            foreach (string line in seven.ToDiagramLines())
                r.Note(line);

            r.Heading("AVL tree: balance guarantee");

            // The same sorted input that turned the plain BST into a 200-level list
            var sequential = new AvlTree<int, string>();
            for (int i = 1; i <= 200; i++)
                sequential.Insert(i, "v" + i);

            string value;
            sequential.TryGetValue(200, out value);
            int sequentialHeight = sequential.Height;
            int sequentialComparisons = sequential.LastOperationComparisons;

            r.Check("Sorted insertion of 200 keys keeps the tree valid and balanced", sequential.Validate());
            r.Check("Height is at most 11 (AVL bound: 1.44 x log2(n + 2))", sequentialHeight <= 11);
            r.Check("Finding key 200 needs no more comparisons than the height",
                sequentialComparisons <= sequentialHeight);
            r.Note(string.Format("Sorted order, AVL: height {0}, {1} comparisons to find key 200, {2} rotations",
                sequentialHeight, sequentialComparisons, sequential.RotationCount));
            r.Note("(the plain BST needed height 200 and 200 comparisons for the same keys)");

            // 37 and 101 share no factors, so i * 37 % 101 visits every value 1..100 exactly once
            var scrambled = new AvlTree<int, string>();
            for (int i = 1; i <= 100; i++)
            {
                int key = (i * 37) % 101;
                scrambled.Insert(key, "v" + key);
            }

            r.Check("100 keys in scrambled order: tree is valid and has 100 entries",
                scrambled.Validate() && scrambled.Count == 100);
            r.Check("In-order traversal returns 1..100",
                Keys(scrambled.InOrder()) == string.Join(",", Enumerable.Range(1, 100)));
            r.Check("Duplicate key is rejected and the tree stays valid",
                !scrambled.Insert(50, "other") && scrambled.Count == 100 && scrambled.Validate());
            r.Check("Range 10..15 gives 10,11,12,13,14,15",
                Keys(scrambled.GetRange(10, 15)) == "10,11,12,13,14,15");
        }

        private static void TestRedBlackTree(Report r)
        {
            r.Heading("Red-black tree: rules and fix-up cases");

            var empty = new RedBlackTree<int, string>();
            r.Check("Empty red-black tree is valid with height 0",
                empty.Validate() && empty.Height == 0 && empty.Count == 0);

            var single = new RedBlackTree<int, string>();
            single.Insert(10, "v10");
            r.Check("A single node is the root and is black",
                single.GetColour(10) == "Black" && single.BlackHeight == 1 && single.Validate());

            // Uncle is empty (black): rotations, never a recolour
            CheckRbRotation(r, "Left-left case (insert 30,20,10): one right rotation",
                new[] { 30, 20, 10 }, 1);
            CheckRbRotation(r, "Right-right case (insert 10,20,30): one left rotation",
                new[] { 10, 20, 30 }, 1);
            CheckRbRotation(r, "Left-right case (insert 30,10,20): double rotation",
                new[] { 30, 10, 20 }, 2);
            CheckRbRotation(r, "Right-left case (insert 10,30,20): double rotation",
                new[] { 10, 30, 20 }, 2);

            // Uncle is red: recolour only. 20,10,30 then 5 makes 10 and 30 black and 20 red (then the root rule makes 20 black)
            var recolour = new RedBlackTree<int, string>();
            foreach (int key in new[] { 20, 10, 30, 5 })
                recolour.Insert(key, "v" + key);

            r.Check("Red uncle (insert 20,10,30,5): recolour only, no rotation",
                Keys(recolour.PreOrder()) == "20,10,5,30" &&
                recolour.RecolourCount == 1 && recolour.RotationCount == 0 &&
                recolour.GetColour(20) == "Black" && recolour.GetColour(10) == "Black" &&
                recolour.GetColour(30) == "Black" && recolour.GetColour(5) == "Red" &&
                recolour.Validate());

            var seven = new RedBlackTree<int, string>();
            for (int i = 1; i <= 7; i++)
                seven.Insert(i, "v" + i);

            r.Check("Inserting 1..7 in order: 3 rotations, 2 recolours, height 4 (AVL gives 3)",
                Keys(seven.PreOrder()) == "2,1,4,3,6,5,7" && seven.Height == 4 &&
                seven.RotationCount == 3 && seven.RecolourCount == 2 &&
                seven.GetColour(2) == "Black" && seven.GetColour(4) == "Red" &&
                seven.Validate());
            r.Note("Diagram after inserting 1..7 in order ((R) = red, (B) = black):");
            foreach (string line in seven.ToDiagramLines())
                r.Note(line);

            r.Heading("Red-black tree: balance guarantee");

            // The same sorted input that turned the plain BST into a 200-level list
            var sequential = new RedBlackTree<int, string>();
            for (int i = 1; i <= 200; i++)
                sequential.Insert(i, "v" + i);

            string value;
            sequential.TryGetValue(200, out value);
            int sequentialHeight = sequential.Height;
            int sequentialComparisons = sequential.LastOperationComparisons;
            double heightBound = 2 * Math.Log(sequential.Count + 1, 2);

            r.Check("Sorted insertion of 200 keys keeps all red-black rules valid", sequential.Validate());
            r.Check("Height is at most 2 x log2(n + 1)", sequentialHeight <= heightBound);
            r.Check("Finding key 200 needs no more comparisons than the height",
                sequentialComparisons <= sequentialHeight);
            r.Check("Never more than 2 rotations per insert on average",
                sequential.RotationCount <= 2 * sequential.Count);
            r.Note(string.Format("Sorted order, red-black: height {0} (limit {1:F1}), {2} comparisons to find key 200, " +
                                 "{3} rotations, {4} recolours, black height {5}",
                sequentialHeight, heightBound, sequentialComparisons,
                sequential.RotationCount, sequential.RecolourCount, sequential.BlackHeight));
            r.Note("(the plain BST needed height 200 and 200 comparisons for the same keys)");

            // 37 and 101 share no factors, so i * 37 % 101 visits every value 1..100 exactly once
            var scrambled = new RedBlackTree<int, string>();
            bool validAfterEveryInsert = true;
            for (int i = 1; i <= 100; i++)
            {
                int key = (i * 37) % 101;
                scrambled.Insert(key, "v" + key);
                if (!scrambled.Validate())
                    validAfterEveryInsert = false;
            }

            r.Check("100 scrambled keys: tree is valid after every single insert and has 100 entries",
                validAfterEveryInsert && scrambled.Count == 100);
            r.Check("In-order traversal returns 1..100",
                Keys(scrambled.InOrder()) == string.Join(",", Enumerable.Range(1, 100)));
            r.Check("Duplicate key is rejected and the tree stays valid",
                !scrambled.Insert(50, "other") && scrambled.Count == 100 && scrambled.Validate());
            r.Check("Range 10..15 gives 10,11,12,13,14,15",
                Keys(scrambled.GetRange(10, 15)) == "10,11,12,13,14,15");

            // The real use: a date-reported index. 7 and 30 share no factors, so every day 0..29 is used once.
            var dates = new RedBlackTree<DateTime, string>();
            DateTime start = new DateTime(2026, 11, 1);
            for (int i = 0; i < 30; i++)
            {
                int day = (i * 7) % 30;
                dates.Insert(start.AddDays(day), "Request " + day);
            }

            string datesInRange = string.Join(",",
                dates.GetRange(start.AddDays(5), start.AddDays(9)).Select(e => e.Key.Day));
            r.Check("Date index: range 6-10 Nov gives days 6,7,8,9,10 and the tree is valid",
                dates.Count == 30 && datesInRange == "6,7,8,9,10" && dates.Validate());
        }

        private static void TestBinaryHeap(Report r)
        {
            r.Heading("Binary heap (priority queue): basics");

            var empty = new BinaryHeap<int>(Comparer<int>.Default);
            int ignored;
            r.Check("Empty heap has count 0, height 0 and is valid",
                empty.Count == 0 && empty.Height == 0 && empty.IsEmpty && empty.Validate());
            r.Check("Empty heap: Peek and Dequeue throw, TryPeek and TryDequeue return false",
                ThrowsInvalidOperation(() => empty.Peek()) &&
                ThrowsInvalidOperation(() => empty.Dequeue()) &&
                !empty.TryPeek(out ignored) && !empty.TryDequeue(out ignored));

            bool nullRejected = false;
            try { new BinaryHeap<int>((Comparison<int>)null); }
            catch (ArgumentNullException) { nullRejected = true; }
            r.Check("A null comparison is rejected with ArgumentNullException", nullRejected);

            // Hand-traced: each new value sifts up past larger parents
            var heap = new BinaryHeap<int>((a, b) => a.CompareTo(b));
            foreach (int value in new[] { 50, 30, 70, 20, 40, 60, 80, 10 })
                heap.Enqueue(value);

            r.Check("Eight values: smallest (10) is on top and the array layout is as expected",
                heap.Count == 8 && heap.Peek() == 10 && heap.Height == 4 && heap.Validate() &&
                string.Join(",", heap.ToList()) == "10,20,60,30,40,70,80,50");

            List<string> levels = heap.ToLevelLines();
            r.Check("Level view has one line per level",
                levels.Count == heap.Height && levels[0] == "Level 1: 10" && levels[1] == "Level 2: 20, 60");
            r.Note("Heap by level (smallest on top, siblings are not sorted):");
            foreach (string line in levels)
                r.Note(line);

            int first = heap.Dequeue();
            r.Check("Dequeue returns 10, then 20 is on top and the heap is still valid",
                first == 10 && heap.Peek() == 20 && heap.Count == 7 && heap.Validate());
            r.Check("Draining the rest gives 20,30,40,50,60,70,80 (sorted)",
                Drain(heap) == "20,30,40,50,60,70,80" && heap.IsEmpty);

            var duplicates = new BinaryHeap<int>(Comparer<int>.Default);
            foreach (int value in new[] { 5, 5, 3, 3 })
                duplicates.Enqueue(value);
            r.Check("Equal items are all kept (5,5,3,3 drains as 3,3,5,5)",
                Drain(duplicates) == "3,3,5,5");

            var maxFirst = new BinaryHeap<int>((a, b) => b.CompareTo(a));
            foreach (int value in new[] { 3, 1, 5, 2, 4 })
                maxFirst.Enqueue(value);
            r.Check("A reversed comparison gives a max-first queue (5,4,3,2,1)",
                maxFirst.Validate() && Drain(maxFirst) == "5,4,3,2,1");

            var wrapper = new MinPriorityQueue<string>((a, b) => a.Length.CompareTo(b.Length));
            foreach (string word in new[] { "pear", "apple", "fig", "banana" })
                wrapper.Enqueue(word);
            r.Check("Part 2 MinPriorityQueue still works on top of the heap (shortest word first)",
                wrapper.Count == 4 && wrapper.Peek() == "fig" &&
                wrapper.Dequeue() == "fig" && wrapper.Dequeue() == "pear" &&
                wrapper.Dequeue() == "apple" && wrapper.Dequeue() == "banana");

            r.Heading("Binary heap: BuildHeap and heap sort");

            // 7 and 20 share no factors, so i * 7 % 20 gives every value 0..19 exactly once
            var scrambled = new List<int>();
            for (int i = 1; i <= 20; i++)
                scrambled.Add((i * 7) % 20);

            var built = new BinaryHeap<int>(Comparer<int>.Default);
            built.BuildHeap(scrambled);
            int buildComparisons = built.LastOperationComparisons;

            r.Check("BuildHeap on 20 scrambled values: valid, count 20, smallest (0) on top",
                built.Count == 20 && built.Validate() && built.Peek() == 0);
            r.Check("BuildHeap needs at most 2n comparisons (O(n))", buildComparisons <= 2 * 20);

            List<int> sorted = built.GetSortedCopy();
            r.Check("GetSortedCopy (heap sort) returns 0..19 and leaves the heap unchanged",
                string.Join(",", sorted) == string.Join(",", Enumerable.Range(0, 20)) &&
                built.Count == 20 && built.Peek() == 0 && built.Validate());
            r.Note(string.Format("BuildHeap on 20 values: {0} comparisons (limit 40)", buildComparisons));

            r.Heading("Binary heap: efficiency");

            // Worst case for a min-heap: every new value is the smallest so far and sifts all the way up
            var big = new BinaryHeap<int>(Comparer<int>.Default);
            for (int i = 1000; i >= 1; i--)
                big.Enqueue(i);

            int insertComparisons = big.LastOperationComparisons;
            r.Check("1000 values inserted in the worst order: valid, 1 on top, height 10",
                big.Count == 1000 && big.Validate() && big.Peek() == 1 && big.Height == 10);
            r.Check("The last (worst-case) insert needs at most height - 1 comparisons",
                insertComparisons <= big.Height - 1);

            int removed = big.Dequeue();
            int removeComparisons = big.LastOperationComparisons;
            r.Check("Dequeue needs at most 2 x (height - 1) comparisons and keeps the heap valid",
                removed == 1 && big.Peek() == 2 && big.Validate() &&
                removeComparisons <= 2 * (big.Height - 1));
            r.Note(string.Format("1000 values: height {0}, worst insert {1} comparisons, dequeue {2} comparisons, {3} swaps so far",
                big.Height, insertComparisons, removeComparisons, big.SwapCount));
        }

        // =====================================================================
        private static void CheckRotation(Report r, string name, int[] insertOrder, int expectedRotations)
        {
            var tree = new AvlTree<int, string>();
            foreach (int key in insertOrder)
                tree.Insert(key, "v" + key);

            // All four cases end with 20 at the root, 10 on its left and 30 on its right
            r.Check(name,
                Keys(tree.PreOrder()) == "20,10,30" &&
                tree.RotationCount == expectedRotations &&
                tree.Validate());
        }

        private static void CheckRbRotation(Report r, string name, int[] insertOrder, int expectedRotations)
        {
            var tree = new RedBlackTree<int, string>();
            foreach (int key in insertOrder)
                tree.Insert(key, "v" + key);

            // All four cases end with a black 20 at the root and red 10 and 30 below it
            r.Check(name,
                Keys(tree.PreOrder()) == "20,10,30" &&
                tree.RotationCount == expectedRotations &&
                tree.RecolourCount == 0 &&
                tree.GetColour(20) == "Black" &&
                tree.GetColour(10) == "Red" &&
                tree.GetColour(30) == "Red" &&
                tree.Validate());
        }

        private static string Drain(BinaryHeap<int> heap)
        {
            var items = new List<int>();
            while (heap.Count > 0)
                items.Add(heap.Dequeue());
            return string.Join(",", items);
        }

        private static bool ThrowsInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return true;
            }
            return false;
        }

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
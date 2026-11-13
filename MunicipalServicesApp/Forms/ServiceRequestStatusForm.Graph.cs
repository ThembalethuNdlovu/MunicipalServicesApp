using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MunicipalServicesApp.DataStructures;
using MunicipalServicesApp.Services;

namespace MunicipalServicesApp.Forms
{
    /// <summary>
    /// The "Service areas" road-network panel: the graph drawn on a map-like canvas, with
    /// Prim and Kruskal minimum spanning trees and breadth-first / depth-first traversal.
    /// This is the second half of ServiceRequestStatusForm (Step 8b).
    /// </summary>
    public partial class ServiceRequestStatusForm
    {
        private enum AreaGraphMode { None, Prim, Kruskal, Breadth, Depth }

        /// <summary>A panel that does not flicker while it is redrawn.</summary>
        private sealed class AreaGraphCanvas : Panel
        {
            public AreaGraphCanvas()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
                BackColor = Color.White;
            }
        }

        // Rough map positions (0..1 across, 0..1 down). Approximate, for readability only.
        private static readonly Dictionary<string, PointF> AreaMapPositions =
            new Dictionary<string, PointF>(StringComparer.OrdinalIgnoreCase)
            {
                { "Diepsloot", new PointF(0.22f, 0.10f) },
                { "Fourways", new PointF(0.50f, 0.12f) },
                { "Midrand", new PointF(0.85f, 0.16f) },
                { "Randburg", new PointF(0.38f, 0.33f) },
                { "Sandton", new PointF(0.62f, 0.42f) },
                { "Roodepoort", new PointF(0.08f, 0.50f) },
                { "Braamfontein", new PointF(0.48f, 0.56f) },
                { "Alexandra", new PointF(0.84f, 0.54f) },
                { "Soweto", new PointF(0.30f, 0.74f) },
                { "Orange Farm", new PointF(0.30f, 0.94f) }
            };

        private const float AreaNodeRadius = 20f;

        private Graph<string> _agGraph;
        private AreaGraphCanvas _agCanvas;
        private ComboBox _agStart;
        private TextBox _agResult;
        private Label _agLegend;
        private ToolTip _agTip;
        private AreaGraphMode _agMode = AreaGraphMode.None;
        private string _agHoverText = string.Empty;

        // Roads of the spanning tree being shown (GraphEdge.Order identifies a road)
        private readonly HashSet<int> _agMstOrders = new HashSet<int>();

        // Visit number of each area in the traversal being shown (1 = start)
        private readonly Dictionary<string, int> _agVisitRank =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, AreaSummary> _agSummaries =
            new Dictionary<string, AreaSummary>(StringComparer.OrdinalIgnoreCase);

        // =====================================================================
        //  Building the panel (called once, from BuildAreasTab)
        // =====================================================================
        partial void BuildAreaGraphPanel(Panel host)
        {
            _agGraph = ServiceAreaCatalog.BuildGraph();
            _agTip = new ToolTip();
            host.Disposed += (s, ev) => _agTip.Dispose();

            // Remove the "added in Step 8b" placeholder label
            while (host.Controls.Count > 0)
            {
                Control placeholder = host.Controls[0];
                host.Controls.RemoveAt(0);
                placeholder.Dispose();
            }

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 130f));

            // ---- toolbar
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = true,
                Padding = new Padding(4)
            };

            bar.Controls.Add(MakeFieldLabel("Start area:"));
            _agStart = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 130,
                Margin = new Padding(2, 4, 2, 2)
            };
            foreach (string area in _agGraph.Vertices)
                _agStart.Items.Add(area);
            if (_agStart.Items.Count > 0)
            {
                int sandton = _agStart.Items.IndexOf("Sandton");
                _agStart.SelectedIndex = sandton >= 0 ? sandton : 0;
            }
            _agStart.SelectedIndexChanged += AgStart_Changed;
            bar.Controls.Add(_agStart);

            Button prim = MakeButton("Prim MST", 90);
            prim.Click += (s, ev) => RunAreaMode(AreaGraphMode.Prim);
            bar.Controls.Add(prim);

            Button kruskal = MakeButton("Kruskal MST", 105);
            kruskal.Click += (s, ev) => RunAreaMode(AreaGraphMode.Kruskal);
            bar.Controls.Add(kruskal);

            Button breadth = MakeButton("Breadth-first", 105);
            breadth.Click += (s, ev) => RunAreaMode(AreaGraphMode.Breadth);
            bar.Controls.Add(breadth);

            Button depth = MakeButton("Depth-first", 95);
            depth.Click += (s, ev) => RunAreaMode(AreaGraphMode.Depth);
            bar.Controls.Add(depth);

            Button clear = MakeButton("Clear", 70);
            clear.Click += (s, ev) => ClearAreaMode();
            bar.Controls.Add(clear);

            // ---- canvas
            _agCanvas = new AreaGraphCanvas { Dock = DockStyle.Fill, Margin = new Padding(0) };
            _agCanvas.Paint += AgCanvas_Paint;
            _agCanvas.MouseClick += AgCanvas_MouseClick;
            _agCanvas.MouseMove += AgCanvas_MouseMove;
            _agCanvas.MouseLeave += (s, ev) => SetAgHover(string.Empty);

            // ---- legend and results
            _agLegend = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8.5f),
                Padding = new Padding(4, 2, 4, 0)
            };

            _agResult = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9f),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            table.Controls.Add(bar, 0, 0);
            table.Controls.Add(_agCanvas, 0, 1);
            table.Controls.Add(_agLegend, 0, 2);
            table.Controls.Add(_agResult, 0, 3);
            host.Controls.Add(table);

            ShowAreaGraphIntro();
            UpdateAreaGraphLegend();
        }

        // =====================================================================
        //  Refreshing (called every time the requests change)
        // =====================================================================
        partial void RefreshAreaGraphPanel()
        {
            if (_agCanvas == null || _agGraph == null)
                return;

            _agSummaries = _service.GetAreaSummaries()
                .ToDictionary(s => s.Area, StringComparer.OrdinalIgnoreCase);

            UpdateAreaGraphLegend();
            _agCanvas.Invalidate();
        }

        private void UpdateAreaGraphLegend()
        {
            AreaSummary unassigned;
            int outside = _agSummaries.TryGetValue(ServiceAreaCatalog.Unassigned, out unassigned)
                ? unassigned.Total
                : 0;

            _agLegend.Text = "Number in a circle = requests not yet resolved (darker = more). " +
                             "Number on a road = km. Click an area to make it the start area. " +
                             "Requests not matched to an area (not on the map): " +
                             outside.ToString(CultureInfo.InvariantCulture) + ".";
        }

        private void ShowAreaGraphIntro()
        {
            int pieces = _agGraph.ConnectedComponents().Count;

            var sb = new StringBuilder();
            sb.AppendLine("SERVICE AREA ROAD NETWORK (weighted, undirected graph)");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0} areas (vertices) | {1} roads (edges) | {2} connected piece(s) | {3} km of road in total",
                _agGraph.VertexCount, _agGraph.EdgeCount, pieces, _agGraph.TotalWeight));
            sb.AppendLine();
            sb.AppendLine("Prim / Kruskal : the cheapest set of roads that still links every area.");
            sb.AppendLine("Breadth-first  : visits areas by how many roads away they are from the start (queue).");
            sb.AppendLine("Depth-first    : follows one road as far as it goes before backing up (stack).");
            _agResult.Text = sb.ToString();
        }

        // =====================================================================
        //  Running an algorithm
        // =====================================================================
        private void AgStart_Changed(object sender, EventArgs e)
        {
            if (_agMode == AreaGraphMode.Prim || _agMode == AreaGraphMode.Breadth || _agMode == AreaGraphMode.Depth)
                RunAreaMode(_agMode);
            else
                _agCanvas.Invalidate();
        }

        private void ClearAreaMode()
        {
            _agMode = AreaGraphMode.None;
            _agMstOrders.Clear();
            _agVisitRank.Clear();
            ShowAreaGraphIntro();
            _agCanvas.Invalidate();
        }

        private void RunAreaMode(AreaGraphMode mode)
        {
            string start = _agStart.SelectedItem as string;
            bool needsStart = mode != AreaGraphMode.Kruskal;

            _agMstOrders.Clear();
            _agVisitRank.Clear();

            if (needsStart && start == null)
            {
                _agMode = AreaGraphMode.None;
                _agResult.Text = "Please choose a start area first.";
                _agCanvas.Invalidate();
                return;
            }

            _agMode = mode;

            switch (mode)
            {
                case AreaGraphMode.Prim:
                case AreaGraphMode.Kruskal:
                    {
                        MstResult<string> primResult = _agGraph.PrimMst(start);
                        MstResult<string> kruskalResult = _agGraph.KruskalMst();
                        MstResult<string> shown = mode == AreaGraphMode.Prim ? primResult : kruskalResult;
                        MstResult<string> other = mode == AreaGraphMode.Prim ? kruskalResult : primResult;

                        foreach (GraphEdge<string> edge in shown.Edges)
                            _agMstOrders.Add(edge.Order);

                        _agResult.Text = DescribeMst(shown, other, start);
                        break;
                    }

                case AreaGraphMode.Breadth:
                    {
                        List<KeyValuePair<string, int>> visits = _agGraph.BreadthFirstWithHops(start);
                        for (int i = 0; i < visits.Count; i++)
                            _agVisitRank[visits[i].Key] = i + 1;

                        _agResult.Text = DescribeBreadthFirst(visits, start);
                        break;
                    }

                default:
                    {
                        List<string> order = _agGraph.DepthFirst(start);
                        for (int i = 0; i < order.Count; i++)
                            _agVisitRank[order[i]] = i + 1;

                        _agResult.Text = DescribeDepthFirst(order, start);
                        break;
                    }
            }

            _agResult.Select(0, 0);
            _agCanvas.Invalidate();
        }

        private static string FormatRoad(GraphEdge<string> edge)
        {
            return edge.From + " - " + edge.To + " (" +
                   edge.Weight.ToString(CultureInfo.InvariantCulture) + " km)";
        }

        private string DescribeMst(MstResult<string> shown, MstResult<string> other, string start)
        {
            bool isPrim = shown.Algorithm == "Prim";
            var sb = new StringBuilder();

            sb.AppendLine("MINIMUM SPANNING TREE - " + shown.Algorithm +
                          (isPrim ? " (starting at " + start + ")" : string.Empty));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0} of {1} roads chosen | total length {2} km | links every area: {3}",
                shown.Edges.Count, _agGraph.EdgeCount, shown.TotalWeight,
                shown.SpansAllVertices ? "yes" : "no"));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Edge comparisons: {0} ({1})",
                shown.EdgeComparisons,
                isPrim ? "min-heap of candidate roads" : "sorting the roads, then union-find to avoid loops"));
            sb.AppendLine("Roads: " + string.Join("; ", shown.Edges.Select(edge => FormatRoad(edge))));
            sb.AppendLine(other.Algorithm + " finds " +
                          other.TotalWeight.ToString(CultureInfo.InvariantCulture) + " km" +
                          (other.TotalWeight == shown.TotalWeight
                              ? " too: same total, different method."
                              : ": the totals differ, check the road data."));
            sb.Append("The roads not drawn thick are not needed to keep every area connected.");
            return sb.ToString();
        }

        private string DescribeBreadthFirst(List<KeyValuePair<string, int>> visits, string start)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BREADTH-FIRST TRAVERSAL from " + start + " (uses a queue)");
            sb.AppendLine(visits.Count.ToString(CultureInfo.InvariantCulture) + " areas reached. " +
                          "Numbers on the map show the visit order.");

            foreach (IGrouping<int, KeyValuePair<string, int>> group in visits.GroupBy(pair => pair.Value))
            {
                string label = group.Key == 0
                    ? "Start"
                    : group.Key.ToString(CultureInfo.InvariantCulture) + (group.Key == 1 ? " road away" : " roads away");

                sb.AppendLine(label.PadRight(15) + ": " + string.Join(", ", group.Select(pair => pair.Key)));
            }

            return sb.ToString();
        }

        private string DescribeDepthFirst(List<string> order, string start)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DEPTH-FIRST TRAVERSAL from " + start + " (uses a stack)");
            sb.AppendLine(order.Count.ToString(CultureInfo.InvariantCulture) + " areas reached. " +
                          "Numbers on the map show the visit order.");
            sb.AppendLine("Order: " + string.Join(" \u2192 ", order));
            return sb.ToString();
        }

        // =====================================================================
        //  Drawing
        // =====================================================================
        private Dictionary<string, PointF> ComputeAreaPoints(Size size)
        {
            const float left = 56f;
            const float right = 56f;
            const float top = 28f;
            const float bottom = 46f;

            float width = Math.Max(50f, size.Width - left - right);
            float height = Math.Max(50f, size.Height - top - bottom);

            var points = new Dictionary<string, PointF>(StringComparer.OrdinalIgnoreCase);
            int fallbackIndex = 0;
            int count = Math.Max(1, _agGraph.VertexCount);

            foreach (string area in _agGraph.Vertices)
            {
                PointF relative;
                if (!AreaMapPositions.TryGetValue(area, out relative))
                {
                    // An area with no map position goes on a circle instead
                    double angle = 2.0 * Math.PI * fallbackIndex / count;
                    fallbackIndex++;
                    relative = new PointF(0.5f + 0.4f * (float)Math.Cos(angle),
                                          0.5f + 0.4f * (float)Math.Sin(angle));
                }

                points[area] = new PointF(left + relative.X * width, top + relative.Y * height);
            }

            return points;
        }

        private static Color Blend(Color from, Color to, float amount)
        {
            amount = Math.Max(0f, Math.Min(1f, amount));
            return Color.FromArgb(
                from.R + (int)((to.R - from.R) * amount),
                from.G + (int)((to.G - from.G) * amount),
                from.B + (int)((to.B - from.B) * amount));
        }

        private void AgCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (_agGraph == null)
                return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Dictionary<string, PointF> points = ComputeAreaPoints(_agCanvas.ClientSize);

            int maxOpen = 0;
            foreach (AreaSummary summary in _agSummaries.Values)
            {
                if (_agGraph.Contains(summary.Area) && summary.Open > maxOpen)
                    maxOpen = summary.Open;
            }

            string start = _agStart.SelectedItem as string;
            bool showStartRing = _agMode == AreaGraphMode.Prim ||
                                 _agMode == AreaGraphMode.Breadth ||
                                 _agMode == AreaGraphMode.Depth;

            using (var thinPen = new Pen(Color.Silver, 1.5f))
            using (var thickPen = new Pen(Brand, 4.5f))
            using (var kmFont = new Font("Segoe UI", 8f))
            using (var nameFont = new Font("Segoe UI", 8.5f))
            using (var countFont = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (var badgeFont = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var center = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            using (var brandBrush = new SolidBrush(Brand))
            using (var whiteBrush = new SolidBrush(Color.White))
            using (var grayBrush = new SolidBrush(Color.DimGray))
            using (var blackBrush = new SolidBrush(Color.Black))
            using (var orangeBrush = new SolidBrush(Color.DarkOrange))
            using (var nameBackBrush = new SolidBrush(Color.FromArgb(205, 255, 255, 255)))
            {
                // 1. Roads
                foreach (GraphEdge<string> edge in _agGraph.Edges)
                {
                    PointF a;
                    PointF b;
                    if (!points.TryGetValue(edge.From, out a) || !points.TryGetValue(edge.To, out b))
                        continue;

                    g.DrawLine(_agMstOrders.Contains(edge.Order) ? thickPen : thinPen, a, b);
                }

                // 2. Road lengths (km), on top of the roads
                foreach (GraphEdge<string> edge in _agGraph.Edges)
                {
                    PointF a;
                    PointF b;
                    if (!points.TryGetValue(edge.From, out a) || !points.TryGetValue(edge.To, out b))
                        continue;

                    bool inTree = _agMstOrders.Contains(edge.Order);
                    string text = edge.Weight.ToString(CultureInfo.InvariantCulture);
                    SizeF size = g.MeasureString(text, kmFont);
                    var box = new RectangleF(
                        (a.X + b.X) / 2f - size.Width / 2f,
                        (a.Y + b.Y) / 2f - size.Height / 2f,
                        size.Width, size.Height);

                    g.FillRectangle(inTree ? brandBrush : whiteBrush, box);
                    g.DrawString(text, kmFont, inTree ? whiteBrush : grayBrush, box.Location);
                }

                // 3. Areas
                foreach (string area in _agGraph.Vertices)
                {
                    PointF p = points[area];

                    int open = 0;
                    AreaSummary summary;
                    if (_agSummaries.TryGetValue(area, out summary))
                        open = summary.Open;

                    float shade = maxOpen == 0 ? 0f : (float)open / maxOpen;
                    Color fill = Blend(Color.FromArgb(236, 241, 238), Brand, shade);
                    var circle = new RectangleF(p.X - AreaNodeRadius, p.Y - AreaNodeRadius,
                                                2f * AreaNodeRadius, 2f * AreaNodeRadius);

                    using (var fillBrush = new SolidBrush(fill))
                        g.FillEllipse(fillBrush, circle);

                    bool isStart = showStartRing && start != null &&
                                   string.Equals(area, start, StringComparison.OrdinalIgnoreCase);
                    using (var outline = new Pen(isStart ? Color.DarkOrange : Color.DimGray, isStart ? 3.5f : 1.5f))
                        g.DrawEllipse(outline, circle);

                    g.DrawString(open.ToString(CultureInfo.InvariantCulture), countFont,
                                 shade > 0.5f ? whiteBrush : blackBrush, circle, center);

                    // Name under the circle
                    SizeF nameSize = g.MeasureString(area, nameFont);
                    var nameBox = new RectangleF(p.X - nameSize.Width / 2f, p.Y + AreaNodeRadius + 1f,
                                                 nameSize.Width, nameSize.Height);
                    g.FillRectangle(nameBackBrush, nameBox);
                    g.DrawString(area, nameFont, blackBrush, nameBox.Location);

                    // Visit-order badge (traversals only)
                    int rank;
                    if (_agVisitRank.TryGetValue(area, out rank))
                    {
                        var badge = new RectangleF(p.X + AreaNodeRadius - 10f, p.Y - AreaNodeRadius - 8f, 18f, 18f);
                        g.FillEllipse(orangeBrush, badge);
                        g.DrawString(rank.ToString(CultureInfo.InvariantCulture), badgeFont, whiteBrush, badge, center);
                    }
                }
            }
        }

        // =====================================================================
        //  Mouse: click an area to make it the start area, hover for details
        // =====================================================================
        private string AreaAt(Point location)
        {
            if (_agGraph == null)
                return null;

            foreach (KeyValuePair<string, PointF> pair in ComputeAreaPoints(_agCanvas.ClientSize))
            {
                float dx = location.X - pair.Value.X;
                float dy = location.Y - pair.Value.Y;
                if (dx * dx + dy * dy <= 22f * 22f)
                    return pair.Key;
            }

            return null;
        }

        private void AgCanvas_MouseClick(object sender, MouseEventArgs e)
        {
            string area = AreaAt(e.Location);
            if (area != null)
                _agStart.SelectedItem = area; // AgStart_Changed re-runs the current algorithm
        }

        private void AgCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            string area = AreaAt(e.Location);
            if (area == null)
            {
                SetAgHover(string.Empty);
                return;
            }

            int total = 0;
            int open = 0;
            AreaSummary summary;
            if (_agSummaries.TryGetValue(area, out summary))
            {
                total = summary.Total;
                open = summary.Open;
            }

            SetAgHover(string.Format(CultureInfo.InvariantCulture,
                "{0}\n{1} request(s), {2} not yet resolved\n{3} road(s) to neighbouring areas",
                area, total, open, _agGraph.Degree(area)));
        }

        private void SetAgHover(string text)
        {
            _agCanvas.Cursor = text.Length > 0 ? Cursors.Hand : Cursors.Default;

            if (text != _agHoverText)
            {
                _agHoverText = text;
                _agTip.SetToolTip(_agCanvas, text);
            }
        }
    }
}
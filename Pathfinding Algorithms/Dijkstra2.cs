using Pathfinding_Algorithms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Pathfinding_Algorithms
{
    public partial class Dijkstra2 : Form
    {
        private string currentMode;
        private List<Circle> circles;
        private Circle startCircle;
        private Circle endCircle;
        private System.Windows.Forms.Timer algorithmTimer;
        private Dictionary<Circle, int> distances;
        private Dictionary<Circle, Circle> previousCircleMap;
        private List<Circle> priorityQueue;
        private bool algorithmRunning;
        private Circle currentCircle;
        private Random random;

        public Dijkstra2()
        {
            InitializeComponent();
            circles = new List<Circle>();
            currentMode = "None";
            algorithmTimer = new System.Windows.Forms.Timer();
            algorithmTimer.Interval = 5;
            algorithmTimer.Tick += AlgorithmTimer_Tick;
            distances = new Dictionary<Circle, int>();
            previousCircleMap = new Dictionary<Circle, Circle>();
            priorityQueue = new List<Circle>();
            random = new Random();
        }

        private void Dijkstra2_Load(object sender, EventArgs e)
        {
        }

        private void pnlGrid_MouseClick(object sender, MouseEventArgs e)
        {
            AddCircle(e.Location);
        }

        private void AddCircle(Point location)
        {
            int radius = 10;
            Circle newCircle = new Circle(location, radius);

            var overlappingCircle = circles.FirstOrDefault(c => c.IsOverlapping(newCircle));
            if (overlappingCircle != null)
            {
                circles.Remove(overlappingCircle);
                pnlGrid.Invalidate(new Rectangle(overlappingCircle.Location.X - radius, overlappingCircle.Location.Y - radius, radius * 2, radius * 2));
            }

            switch (currentMode)
            {
                case "Add Circle":
                    newCircle.Color = Color.LightGray;
                    circles.Add(newCircle);
                    break;
                case "Add Start":
                    if (startCircle != null)
                    {
                        circles.Remove(startCircle);
                        pnlGrid.Invalidate(new Rectangle(startCircle.Location.X - radius, startCircle.Location.Y - radius, radius * 2, radius * 2));
                    }
                    newCircle.Color = Color.Green;
                    startCircle = newCircle;
                    circles.Add(newCircle);
                    break;
                case "Add Finish":
                    if (endCircle != null)
                    {
                        circles.Remove(endCircle);
                        pnlGrid.Invalidate(new Rectangle(endCircle.Location.X - radius, endCircle.Location.Y - radius, radius * 2, radius * 2));
                    }
                    newCircle.Color = Color.Red;
                    endCircle = newCircle;
                    circles.Add(newCircle);
                    break;
            }

            pnlGrid.Invalidate(new Rectangle(location.X - radius, location.Y - radius, radius * 2, radius * 2));
        }

        private void pnlGrid_Paint(object sender, PaintEventArgs e)
        {
            foreach (var circle in circles)
            {
                circle.Draw(e.Graphics);
            }

            if (!algorithmRunning && endCircle != null && startCircle != null)
            {
                DrawPath(e.Graphics);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            currentMode = "Add Circle";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            currentMode = "Add Start";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            currentMode = "Add Finish";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (startCircle == null || endCircle == null)
            {
                MessageBox.Show("Please add both start and end circles.");
                return;
            }

            StartDijkstraAlgorithm();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            AddRandomCircles(200);
        }

        private void AddRandomCircles(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Point randomLocation = new Point(random.Next(pnlGrid.Width), random.Next(pnlGrid.Height));
                currentMode = "Add Circle";
                AddCircle(randomLocation);
            }
        }

        private void StartDijkstraAlgorithm()
        {
            distances.Clear();
            previousCircleMap.Clear();
            priorityQueue.Clear();
            foreach (var circle in circles)
            {
                distances[circle] = int.MaxValue;
            }

            distances[startCircle] = 0;
            priorityQueue.Add(startCircle);

            algorithmRunning = true;
            algorithmTimer.Start();
        }

        private void AlgorithmTimer_Tick(object sender, EventArgs e)
        {
            if (priorityQueue.Count == 0)
            {
                algorithmTimer.Stop();
                algorithmRunning = false;
                pnlGrid.Invalidate();
                return;
            }

            currentCircle = priorityQueue.OrderBy(c => distances[c]).First();
            priorityQueue.Remove(currentCircle);

            if (currentCircle == endCircle)
            {
                algorithmTimer.Stop();
                algorithmRunning = false;
                pnlGrid.Invalidate();
                return;
            }

            VisitNode(currentCircle);
            pnlGrid.Invalidate();
            pnlGrid.Update();
        }

        private void VisitNode(Circle circle)
        {
            foreach (var neighbor in GetNeighbors(circle))
            {
                int distance = GetDistance(circle, neighbor);
                int tentativeDistance = distances[circle] + distance;

                if (tentativeDistance < distances[neighbor])
                {
                    distances[neighbor] = tentativeDistance;
                    previousCircleMap[neighbor] = circle;
                    if (!priorityQueue.Contains(neighbor))
                    {
                        priorityQueue.Add(neighbor);
                    }
                }
            }

            if (circle != startCircle && circle != endCircle)
            {
                circle.Color = Color.Orange;
                pnlGrid.Invalidate(new Rectangle(circle.Location.X - circle.Radius, circle.Location.Y - circle.Radius, circle.Radius * 2, circle.Radius * 2));
            }
        }

        private void DrawLine(Circle c1, Circle c2, Color color, Graphics g)
        {
            if (c1 == null || c2 == null || g == null) return;

            if (c1.Location.IsEmpty || c2.Location.IsEmpty) return;

            if (c1.Location.X < 0 || c1.Location.Y < 0 || c2.Location.X < 0 || c2.Location.Y < 0) return;

            using (Pen pen = new Pen(color, 2))
            {
                try
                {
                    g.DrawLine(pen, c1.Location, c2.Location);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error drawing line: {ex.Message}\nFrom: {c1.Location}\nTo: {c2.Location}");
                }
            }
        }

        private IEnumerable<Circle> GetNeighbors(Circle circle)
        {
            int maxDistance = 50; // Komşuları belirlemek için mesafe sınırı
            return circles.Where(c => c != circle && GetDistance(c, circle) <= maxDistance);
        }

        private void DrawPath(Graphics g)
        {
            if (g == null) return;

            Circle current = endCircle;
            List<Circle> path = new List<Circle>();

            while (current != startCircle)
            {
                if (!previousCircleMap.ContainsKey(current)) break;
                path.Add(current);
                current = previousCircleMap[current];
            }
            path.Add(startCircle);

            path.Reverse();

            for (int i = 0; i < path.Count - 1; i++)
            {
                DrawLine(path[i], path[i + 1], Color.Blue, g);
            }
        }

        private int GetDistance(Circle c1, Circle c2)
        {
            int dx = c1.Location.X - c2.Location.X;
            int dy = c1.Location.Y - c2.Location.Y;
            return (int)Math.Sqrt(dx * dx + dy * dy);
        }

        public class Circle
        {
            public Point Location { get; set; }
            public int Radius { get; set; }
            public Color Color { get; set; }

            public Circle(Point location, int radius)
            {
                Location = location;
                Radius = radius;
                Color = Color.LightGray;
            }

            public void Draw(Graphics g)
            {
                using (Brush brush = new SolidBrush(Color))
                {
                    g.FillEllipse(brush, Location.X - Radius, Location.Y - Radius, Radius * 2, Radius * 2);
                }
            }

            public bool IsOverlapping(Circle other)
            {
                int dx = Location.X - other.Location.X;
                int dy = Location.Y - other.Location.Y;
                int distance = (int)Math.Sqrt(dx * dx + dy * dy);
                return distance < (Radius + other.Radius);
            }
        }
    }
}

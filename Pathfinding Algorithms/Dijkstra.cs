using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pathfinding_Algorithms
{
    public partial class Dijkstra : Form
    {
        public class Node
        {
            public int X { get; set; }
            public int Y { get; set; }
            public bool IsObstacle { get; set; } = false;
            public bool Visited { get; set; } = false;
            public int Distance { get; set; } = int.MaxValue;
            public Node Previous { get; set; } = null;

            public List<Edge> Edges { get; set; }
            public int Weight { get; set; } = 1;

            public Node(int x, int y)
            {
                X = x;
                Y = y;
                Edges = new List<Edge>();
            }
        }

        public class Edge
        {
            public Node Target { get; set; }
            public int Weight { get; set; }

            public Edge(Node target, int weight)
            {
                Target = target;
                Weight = weight;
            }
        }

        private System.Windows.Forms.Timer algorithmTimer = new System.Windows.Forms.Timer();
        private string currentMode;
        private PictureBox[,] grid;
        private const int gridWidth = 90;
        private const int gridHeight = 40;
        private PictureBox startNode = null;
        private PictureBox endNode = null;
        private SortedSet<Node> priorityQueue;
        private Node current = null;
        private bool algorithmRunning = false;
        private Dictionary<PictureBox, Node> pictureBoxToNodeMap = new Dictionary<PictureBox, Node>();
        private bool isMouseDown = false;

        public Dijkstra()
        {
            InitializeComponent();
            InitializeGrid();
            currentMode = "None";
            algorithmTimer.Interval = 5;
            algorithmTimer.Tick += new EventHandler(AlgorithmTimer_Tick);
        }

        private void AlgorithmTimer_Tick(object sender, EventArgs e)
        {
            if (!algorithmRunning || current == null)
            {
                algorithmTimer.Stop();
                CleanupAfterAlgorithm();
                return;
            }

            if (current == pictureBoxToNodeMap[endNode])
            {
                algorithmTimer.Stop();
                TraceBackPath();
                algorithmRunning = false;
                CleanupAfterAlgorithm();
            }
            else
            {
                VisitNode(current);
                current = GetNextNode();
            }
        }

        private void TraceBackPath()
        {
            var temp = pictureBoxToNodeMap[endNode];
            while (temp.Previous != null)
            {
                temp = temp.Previous;
                var pic = grid[temp.X, temp.Y];
                if (temp != pictureBoxToNodeMap[startNode])
                {
                    pic.BackColor = Color.Blue;
                    UpdateNodeLabel(pic, temp.Distance.ToString());
                }
                Application.DoEvents();
            }
        }
        private void VisitNode(Node node)
        {
            node.Visited = true;
            var currentPic = grid[node.X, node.Y];

            if (node != pictureBoxToNodeMap[startNode] && node != pictureBoxToNodeMap[endNode])
            {
                Action updateAction = () => {
                    currentPic.BackColor = Color.Orange;
                    UpdateNodeLabel(currentPic, node.Distance.ToString());
                };

                currentPic.Invoke(updateAction);
            }

            foreach (var edge in node.Edges.Where(e => !e.Target.IsObstacle && !e.Target.Visited))
            {
                int altDistance = node.Distance + edge.Weight;
                if (altDistance < edge.Target.Distance)
                {
                    edge.Target.Distance = altDistance;
                    edge.Target.Previous = node;

                    if (priorityQueue.Contains(edge.Target))
                    {
                        priorityQueue.Remove(edge.Target);
                    }
                    priorityQueue.Add(edge.Target);
                }
            }
        }


        private void CleanupAfterAlgorithm()
        {
            priorityQueue.Clear();
            current = null;
            algorithmRunning = false;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private Node GetNextNode()
        {
            if (!priorityQueue.Any())
                return null;

            var nextNode = priorityQueue.Min;
            priorityQueue.Remove(nextNode);
            return nextNode;
        }

        public void DijkstraAlgorithm()
        {
            current = pictureBoxToNodeMap[startNode];
            current.Distance = 0;
            priorityQueue = new SortedSet<Node>(Comparer<Node>.Create((a, b) => a.Distance.CompareTo(b.Distance) != 0 ? a.Distance.CompareTo(b.Distance) : (a.X * gridWidth + a.Y).CompareTo(b.X * gridWidth + b.Y)));
            priorityQueue.Add(current);

            algorithmRunning = true;
            algorithmTimer.Start();
        }

        private void Dijkstra_Load(object sender, EventArgs e)
        {

        }

        private void InitializeGrid()
        {
            grid = new PictureBox[gridWidth, gridHeight];
            int picBoxSize = 20;

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    var pic = new PictureBox
                    {
                        BackColor = Color.LightGray,
                        Width = picBoxSize,
                        Height = picBoxSize,
                        Location = new Point(x * picBoxSize, y * picBoxSize),
                        BorderStyle = BorderStyle.FixedSingle
                    };
                    var node = new Node(x, y);
                    pictureBoxToNodeMap[pic] = node;
                    pic.MouseDown += (sender, e) => Pic_MouseDown(sender, e, node);
                    pic.MouseUp += Pic_MouseUp;
                    pic.MouseMove += (sender, e) => Pic_MouseMove(sender, e, node);
                    pnlGrid.Controls.Add(pic);
                    grid[x, y] = pic;
                }
            }

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    var node = pictureBoxToNodeMap[grid[x, y]];

                    if (x > 0) node.Edges.Add(new Edge(pictureBoxToNodeMap[grid[x - 1, y]], 1));
                    if (x < gridWidth - 1) node.Edges.Add(new Edge(pictureBoxToNodeMap[grid[x + 1, y]], 1));
                    if (y > 0) node.Edges.Add(new Edge(pictureBoxToNodeMap[grid[x, y - 1]], 1));
                    if (y < gridHeight - 1) node.Edges.Add(new Edge(pictureBoxToNodeMap[grid[x, y + 1]], 1));
                }
            }
        }

        private void Pic_MouseDown(object sender, MouseEventArgs e, Node node)
        {
            isMouseDown = true;
            var pic = sender as PictureBox;
            if (pic != null) HandleNode(pic, node);
        }

        private void Pic_MouseUp(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
        }

        private void Pic_MouseMove(object sender, MouseEventArgs e, Node node)
        {
            if (isMouseDown)
            {
                var pic = sender as PictureBox;
                if (pic != null) HandleNode(pic, node);
            }
        }

        private void HandleNode(PictureBox pic, Node node)
        {
            switch (currentMode)
            {
                case "Obstacle":
                    node.IsObstacle = true;
                    pic.BackColor = Color.Black;
                    break;
                case "Start":
                    if (startNode != null) startNode.BackColor = Color.LightGray;
                    startNode = pic;
                    pic.BackColor = Color.Green;
                    break;
                case "End":
                    if (endNode != null) endNode.BackColor = Color.LightGray;
                    endNode = pic;
                    pic.BackColor = Color.Red;
                    break;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            currentMode = "Start";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            currentMode = "End";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            currentMode = "Obstacle";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (startNode == null || endNode == null)
            {
                MessageBox.Show("Please select start and end nodes.");
                return;
            }
            DijkstraAlgorithm();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            foreach (var pic in pictureBoxToNodeMap.Keys)
            {
                var node = pictureBoxToNodeMap[pic];
                node.IsObstacle = false;
                node.Visited = false;
                node.Distance = int.MaxValue;
                node.Previous = null;
                pic.BackColor = Color.LightGray;
            }
            startNode = null;
            endNode = null;
            GenerateMaze();
        }

        private void GenerateMaze()
        {
            int width = gridWidth;
            int height = gridHeight;

            bool[,] maze = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    maze[x, y] = false;
                }
            }

            Stack<Point> stack = new Stack<Point>();
            Random rand = new Random();
            Point start = new Point(rand.Next(width / 2) * 2, rand.Next(height / 2) * 2);
            stack.Push(start);
            maze[start.X, start.Y] = true;

            while (stack.Count > 0)
            {
                Point current = stack.Peek();
                List<Point> neighbors = new List<Point>();

                if (current.X > 1 && !maze[current.X - 2, current.Y])
                    neighbors.Add(new Point(current.X - 2, current.Y));
                if (current.X < width - 2 && !maze[current.X + 2, current.Y])
                    neighbors.Add(new Point(current.X + 2, current.Y));
                if (current.Y > 1 && !maze[current.X, current.Y - 2])
                    neighbors.Add(new Point(current.X, current.Y - 2));
                if (current.Y < height - 2 && !maze[current.X, current.Y + 2])
                    neighbors.Add(new Point(current.X, current.Y + 2));

                if (neighbors.Count > 0)
                {
                    Point next = neighbors[rand.Next(neighbors.Count)];
                    maze[next.X, next.Y] = true;
                    maze[(current.X + next.X) / 2, (current.Y + next.Y) / 2] = true;
                    stack.Push(next);
                }
                else
                {
                    stack.Pop();
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!maze[x, y])
                    {
                        var pic = grid[x, y];
                        var node = pictureBoxToNodeMap[pic];
                        node.IsObstacle = true;
                        pic.BackColor = Color.Black;
                    }
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            foreach (var pic in pictureBoxToNodeMap.Keys)
            {
                var node = pictureBoxToNodeMap[pic];
                node.IsObstacle = false;
                node.Visited = false;
                node.Distance = int.MaxValue;
                node.Previous = null;
                pic.BackColor = Color.LightGray;
            }
            startNode = null;
            endNode = null;
            AutoBlock();
        }

        private void AutoBlock()
        {
            Random rand = new Random();
            int wallDensity = 30;

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    if (rand.Next(100) < wallDensity)
                    {
                        var pic = grid[x, y];
                        var node = pictureBoxToNodeMap[pic];
                        node.IsObstacle = true;
                        pic.BackColor = Color.Black;
                    }
                }
            }
        }
        private void UpdateNodeLabel(PictureBox pic, string text)
        {
            Label lbl;
            if (pic.Controls.Count == 0)
            {
                lbl = new Label();
                pic.Controls.Add(lbl);
            }
            else
            {
                lbl = (Label)pic.Controls[0];
            }

            lbl.Text = text;
            lbl.AutoSize = true; 
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.Dock = DockStyle.Fill; 
            lbl.BackColor = Color.Transparent;
        }

    }
}

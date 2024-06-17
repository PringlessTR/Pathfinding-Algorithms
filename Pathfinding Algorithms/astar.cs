using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Pathfinding_Algorithms.astar;

namespace Pathfinding_Algorithms
{
    public partial class astar : Form
    {
        private bool mazeMode = false;
        public class Node : IComparable<Node>
        {

            public int X { get; set; }
            public int Y { get; set; }
            public bool IsObstacle { get; set; } = false;
            public bool Visited { get; set; } = false;

            public int G { get; set; } = int.MaxValue;
            public int H { get; set; } = 0;
            public int F { get { return G + H; } }

            public Node Previous { get; set; } = null;
            public List<Edge> Edges { get; set; }

            public Node(int x, int y)
            {
                X = x;
                Y = y;
                Edges = new List<Edge>();
            }

            public int CompareTo(Node other)
            {
                int compare = F.CompareTo(other.F);
                if (compare == 0)
                {
                    compare = H.CompareTo(other.H);
                }
                return compare;
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
        public astar()
        {
            InitializeComponent();
            InitializeGrid();
            currentMode = "None";
            algorithmTimer.Interval = 5;
        }

        private void astar_Load(object sender, EventArgs e)
        {
        }
        private void AlgorithmTimer_Tick(object sender, EventArgs e)
        {
            int corridorWidth = (int)((Timer)sender).Tag;
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
                VisitNode(current, corridorWidth);
                current = GetNextNode();
                if (current == null)
                {
                    algorithmTimer.Stop();
                    algorithmRunning = false;
                    if (!mazeMode && corridorWidth < maxCorridorWidth)
                    {
                        int newCorridorWidth = corridorWidth + 1;
                        ClearNodes(); // Clear previous data
                        algorithmTimer.Tick -= AlgorithmTimer_Tick;
                        AStarAlgorithm(newCorridorWidth);
                    }
                    else
                    {
                        MessageBox.Show("No path found.");
                    }
                }
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
                    UpdateNodeLabel(pic, temp.G.ToString());
                }
                Application.DoEvents();
            }
        }
        private void VisitNode(Node node, int corridorWidth)
        {
            if (node.Visited)
                return;

            node.Visited = true;
            var pic = grid[node.X, node.Y];
            if (pic != startNode && pic != endNode)
            {
                pic.BackColor = Color.Orange;
                UpdateNodeLabel(pic, node.G.ToString());
            }

            foreach (var edge in node.Edges)
            {
                Node targetNode = pictureBoxToNodeMap[endNode];
                Node neighbor = edge.Target;
                if (neighbor.IsObstacle || neighbor.Visited)
                    continue;

                if (!mazeMode && !IsWithinCorridor(pictureBoxToNodeMap[startNode], targetNode, neighbor, corridorWidth))
                    continue;

                int tentativeG = node.G + edge.Weight;
                if (tentativeG < neighbor.G)
                {
                    neighbor.G = tentativeG;
                    neighbor.H = CalculateHeuristic(neighbor, targetNode);
                    neighbor.Previous = node;

                    if (priorityQueue.Contains(neighbor))
                    {
                        priorityQueue.Remove(neighbor);
                    }
                    priorityQueue.Add(neighbor);
                }
            }
        }

        private bool IsWithinCorridor(Node start, Node end, Node point, int corridorWidth)
        {
            float distance = DistanceToLine(start, end, point);
            return distance <= corridorWidth;
        }
        private float DistanceToLine(Node start, Node end, Node point)
        {
            float A = point.X - start.X;
            float B = point.Y - start.Y;
            float C = end.X - start.X;
            float D = end.Y - start.Y;

            float dot = A * C + B * D;
            float len_sq = C * C + D * D;
            float param = dot / len_sq;

            float xx, yy;

            if (param < 0 || (start.X == end.X && start.Y == end.Y))
            {
                xx = start.X;
                yy = start.Y;
            }
            else if (param > 1)
            {
                xx = end.X;
                yy = end.Y;
            }
            else
            {
                xx = start.X + param * C;
                yy = start.Y + param * D;
            }

            float dx = point.X - xx;
            float dy = point.Y - yy;
            return (float)Math.Sqrt(dx * dx + dy * dy);
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
        private void AStarAlgorithm(int corridorWidth)
        {
            mazeMode = IsMazeMode();

            Node start = pictureBoxToNodeMap[startNode];
            start.G = 0;
            start.H = CalculateHeuristic(start, pictureBoxToNodeMap[endNode]);
            priorityQueue = new SortedSet<Node>(Comparer<Node>.Create((a, b) =>
                a.F.CompareTo(b.F) != 0 ? a.F.CompareTo(b.F) :
                (a.X * gridWidth + a.Y).CompareTo(b.X * gridWidth + b.Y)));
            priorityQueue.Add(start);
            current = start;

            algorithmRunning = true;
            algorithmTimer.Tag = corridorWidth;
            algorithmTimer.Tick += AlgorithmTimer_Tick;
            algorithmTimer.Start();
        }

        private bool IsMazeMode()
        {
            int obstacleCount = 0;
            int totalCells = gridWidth * gridHeight;

            foreach (var node in pictureBoxToNodeMap.Values)
            {
                if (node.IsObstacle)
                {
                    obstacleCount++;
                }
            }

            double density = (double)obstacleCount / totalCells;
            return density > 0.3;
        }
        private int CalculateHeuristic(Node a, Node b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            return Math.Max(dx, dy);
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
        private int initialCorridorWidth = 4;
        private int maxCorridorWidth = 10;
        private void button2_Click(object sender, EventArgs e)
        {
            if (startNode == null || endNode == null)
            {
                MessageBox.Show("Please select start and end nodes.");
                return;
            }
            if (algorithmRunning)
            {
                MessageBox.Show("Algorithm is already running.");
                return;
            }
            ClearNodes();
            AStarAlgorithm(initialCorridorWidth);
        }
        private void ClearNodes()
        {
            foreach (var pic in pictureBoxToNodeMap.Keys)
            {
                var node = pictureBoxToNodeMap[pic];
                if (node.Visited || node.G != int.MaxValue)
                {
                    node.Visited = false;
                    node.G = int.MaxValue;
                    node.Previous = null;
                    pic.BackColor = Color.LightGray;
                }
            }
            priorityQueue = new SortedSet<Node>(Comparer<Node>.Create((a, b) =>
                a.F.CompareTo(b.F) != 0 ? a.F.CompareTo(b.F) :
                (a.X * gridWidth + a.Y).CompareTo(b.X * gridWidth + b.Y)));
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
        private void button1_Click_1(object sender, EventArgs e)
        {
            currentMode = "Obstacle";
        }

        private void button6_Click_2(object sender, EventArgs e)
        {
            foreach (var pic in pictureBoxToNodeMap.Keys)
            {
                var node = pictureBoxToNodeMap[pic];
                node.IsObstacle = false;
                node.Visited = false;
                node.G = int.MaxValue;
                node.Previous = null;
                pic.BackColor = Color.LightGray;
            }
            startNode = null;
            endNode = null;
            AutoBlock();
        }

        private void button5_Click_2(object sender, EventArgs e)
        {
            foreach (var pic in pictureBoxToNodeMap.Keys)
            {
                var node = pictureBoxToNodeMap[pic];
                node.IsObstacle = false;
                node.Visited = false;
                node.G = int.MaxValue;
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
        private void AutoBlock()
        {
            Random rand = new Random();
            int numShapes = 20;
            int shapeLength = 5;
            int lShapeLength = 7;

            for (int i = 0; i < numShapes; i++)
            {
                int shapeType = rand.Next(3);
                int startX = rand.Next(0, gridWidth - shapeLength);
                int startY = rand.Next(0, gridHeight - shapeLength);

                switch (shapeType)
                {
                    case 0:
                        bool horizontal = rand.Next(2) == 0;
                        for (int j = 0; j < shapeLength; j++)
                        {
                            int x = horizontal ? startX + j : startX;
                            int y = horizontal ? startY : startY + j;

                            if (x < gridWidth && y < gridHeight)
                            {
                                var pic = grid[x, y];
                                var node = pictureBoxToNodeMap[pic];
                                node.IsObstacle = true;
                                pic.BackColor = Color.Black;
                            }
                        }
                        break;

                    case 1:
                        for (int j = 0; j < lShapeLength; j++)
                        {
                            if (startX + j < gridWidth && startY < gridHeight)
                            {
                                var pic = grid[startX + j, startY];
                                var node = pictureBoxToNodeMap[pic];
                                node.IsObstacle = true;
                                pic.BackColor = Color.Black;
                            }
                        }
                        for (int j = 0; j < lShapeLength; j++)
                        {
                            if (startX < gridWidth && startY + j < gridHeight)
                            {
                                var pic = grid[startX, startY + j];
                                var node = pictureBoxToNodeMap[pic];
                                node.IsObstacle = true;
                                pic.BackColor = Color.Black;
                            }
                        }
                        break;

                    case 2:
                        for (int j = 0; j < shapeLength; j++)
                        {
                            if (startX + j < gridWidth && startY < gridHeight)
                            {
                                var pic = grid[startX + j, startY];
                                var node = pictureBoxToNodeMap[pic];
                                node.IsObstacle = true;
                                pic.BackColor = Color.Black;
                            }
                        }
                        for (int j = 0; j < shapeLength; j++)
                        {
                            if (startX < gridWidth && startY + j < gridHeight)
                            {
                                var pic = grid[startX, startY + j];
                                var node = pictureBoxToNodeMap[pic];
                                node.IsObstacle = true;
                                pic.BackColor = Color.Black;
                            }
                        }
                        for (int j = 0; j < shapeLength; j++)
                        {
                            if (startX + j < gridWidth && startY + j < gridHeight)
                            {
                                var pic = grid[startX + j, startY + j];
                                var node = pictureBoxToNodeMap[pic];
                                node.IsObstacle = true;
                                pic.BackColor = Color.Black;
                            }
                        }
                        break;
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

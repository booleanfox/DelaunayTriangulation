using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class MainForm : Form
    {
        DelaunayTriangulation delaunay; 
        List<Triangle> triangulated = new List<Triangle>();
        Dictionary<Triangle, Circle> triangulatedWithCircles = new Dictionary<Triangle, Circle>();

        Triangle selectedTriangle = null;
        List<Point2D> points = new List<Point2D>();
        Point2D tempPoint = new Point2D();
        bool released = true;
                
        public MainForm()
        {
            InitializeComponent();
        }

        public void DrawLines(List<Triangle>Triangles, Graphics g)
        {
            foreach (Triangle t in Triangles)
            {
                g.DrawLine(Pens.Black, t.a.x, t.a.y, t.b.x, t.b.y);
                g.DrawLine(Pens.Black, t.b.x, t.b.y, t.c.x, t.c.y);
                g.DrawLine(Pens.Black, t.c.x, t.c.y, t.a.x, t.a.y);

                if (selectedTriangle == t)
                {
                    Circle c = delaunay.GetCircles(new List<Triangle>() { selectedTriangle })[t];
                    float x = c.center.x - c.radius;
                    float y = c.center.y - c.radius;
                    g.DrawEllipse(Pens.LightGray, x, y, 2 * c.radius, 2 * c.radius);
                }
                if (selectedTriangle != null) continue;
                {
                    if (triangulatedWithCircles.ContainsKey(t))
                    {
                        Circle c = triangulatedWithCircles[t];
                        float x = c.center.x - c.radius;
                        float y = c.center.y - c.radius;
                        g.DrawEllipse(Pens.LightGray, x, y, 2 * c.radius, 2 * c.radius);
                    }
                }    
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(pictureBox1.BackColor);
            if (triangulated!=null && triangulated.Count > 0)
                DrawLines(triangulated, e.Graphics);

            if(points.Count > 0)
                foreach(Point2D p in points)
                {
                    e.Graphics.DrawEllipse(Pens.Black,p.x,p.y,2,2);
                }
        }

        private void button3_Click(object sender, EventArgs e)  // кнопка erase
        {
            points.Clear();
            selectedTriangle = null;
            if(triangulated != null)
                triangulated.Clear();
            Refresh();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (released)
                    released = false;
                points.Add(new Point2D(e.X, e.Y));
                Refresh();
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                released = true;
                if (points.Count > 2)
                {
                    selectedTriangle = null;
                    delaunay = new DelaunayTriangulation(points);
                    triangulated = delaunay.Triangulate();

                    triangulatedWithCircles = delaunay.GetCircles(triangulated);
                    Refresh();
                }
                Refresh();
            }
        }

   
        private void button1_Click(object sender, EventArgs e)      // кнпка cancel
        {   
            if (points.Count > 0)
            {
                Point2D p = points.Last();
                points.Remove(p);
                if (points.Count == 2)
                {
                    selectedTriangle = null;
                    if (triangulated != null)
                        triangulated.Clear();
                }
                else
                {
                    if (points.Count > 2)
                    {
                        selectedTriangle = null;
                        delaunay = new DelaunayTriangulation(points);
                        triangulated = delaunay.Triangulate();

                        triangulatedWithCircles = delaunay.GetCircles(triangulated);
                        Refresh();
                    }
                }
            }
            Refresh();
        }
    }

    public class DelaunayTriangulation
    {
        List<Point2D> points;
        List<Point2D> leftOverPoints = new List<Point2D>();
        List<Point2D> pointsUsed = new List<Point2D>();
        internal List<Triangle> res = new List<Triangle>();

        public DelaunayTriangulation(List<Point2D> points)
        {
            this.points = points;
            for (int i = 0; i < points.Count; i++)
            {
                leftOverPoints.Add((Point2D)points[i].Clone());
            }
        }

        public List<Triangle> Triangulate()
        {
            res.Clear();
            Triangle root = new Triangle();
            Pair pair;
            List<Point2D> leftOverPointsTemp = new List<Point2D>(leftOverPoints);
            List<Point2D> leftOverPoints2 = new List<Point2D>(leftOverPoints);
            foreach (Point2D[] p in GetRandomPoints())
            {
                if (!HasPointsInside(p[0], p[1], p[2]))
                {
                    root = new Triangle(p[0], p[1], p[2]);
                    res.Add(root);
                    leftOverPointsTemp.Remove(p[0]);
                    leftOverPointsTemp.Remove(p[1]);
                    leftOverPointsTemp.Remove(p[2]);

                    pointsUsed.Add(p[0]);
                    pointsUsed.Add(p[1]);
                    pointsUsed.Add(p[2]);

                    pair = Triangulate(leftOverPointsTemp, res);
                    if (pair.ok)
                        return pair.res;
                    else
                    {
                        res.RemoveAt(0);
                        pointsUsed.RemoveAt(pointsUsed.Count - 1);
                        pointsUsed.RemoveAt(pointsUsed.Count - 1);
                        pointsUsed.RemoveAt(pointsUsed.Count - 1);

                        leftOverPointsTemp = leftOverPoints2;
                    }
                }
            }
            return null;
        }

        private Pair Triangulate(List<Point2D> leftOverPoints, List<Triangle> res)
        {
            List<Triangle> temp = null;
            Point2D newPoint = new Point2D();
            while (leftOverPoints.Count > 0)
            {
                for (int i = 0; i < leftOverPoints.Count; i++)
                {
                    temp = GetNextTriangles(leftOverPoints[i]);
                    if (temp.Count > 0)
                    {
                        res.AddRange(temp);
                        newPoint = leftOverPoints[i];
                        leftOverPoints.RemoveAt(i);
                        pointsUsed.Add(newPoint);
                        Pair p = Triangulate(leftOverPoints, res);
                        if (p.ok)
                            return p;
                        else
                        {
                            res.RemoveRange(res.Count - temp.Count, temp.Count);
                            leftOverPoints.Insert(i, newPoint);
                            pointsUsed.Remove(newPoint);
                        }
                    }
                }
                return new Pair(res, false);
            }
            return new Pair(res, true);
        }

        private List<Triangle> GetNextTriangles(Point2D newPoint)
        {
            List<Triangle> result = new List<Triangle>();
            foreach (Point2D[] p in GetPairRandomPoints())
            {
                if (!p[0].Equals(newPoint) && !p[1].Equals(newPoint))
                {
                    if (!HasPointsInside(p[0], p[1], newPoint))
                    {
                        result.Add(new Triangle(p[0], p[1], newPoint));
                    }
                }
            }
            return result;
        }

        private bool HasPointsInside(Point2D point2D, Point2D point2D_2, Point2D point2D_3)
        {
            List<Triangle> list = new List<Triangle>();
            Triangle t = new Triangle(point2D, point2D_2, point2D_3);
            list.Add(t);
            Dictionary<Triangle, Circle> dict = GetCircles(list);
            foreach (Point2D p in points)
            {
                if (!p.Equals(point2D) && !p.Equals(point2D_2) && !p.Equals(point2D_3))
                {
                    if (Math.Pow(p.x - dict[t].center.x, 2) + Math.Pow(p.y - dict[t].center.y, 2) <= Math.Pow(dict[t].radius, 2))
                        return true;
                }
            }
            return false;
        }


        IEnumerable<Point2D[]> GetRandomPoints()
        {
            // ищет все возможные треугольники
            for (int i = 0; i < points.Count; i++)
                for (int j = i + 1; j < points.Count; j++)
                    for (int k = j + 1; k < points.Count; k++)
                        yield return new Point2D[3] { points[i], points[j], points[k] };
        }

        IEnumerable<Point2D[]> GetPairRandomPoints()
        {
            // проверяет все пары возможных точек
            for (int i = 0; i < pointsUsed.Count; i++)
                for (int j = i + 1; j < pointsUsed.Count; j++)
                    yield return new Point2D[2] { pointsUsed[i], pointsUsed[j] };
        }

        public Dictionary<Triangle, Circle> GetCircles(List<Triangle> Triangles)
        {
            Dictionary<Triangle, Circle> res = new Dictionary<Triangle, Circle>();
            Rect r1, r2, r3, r4;
            Point2D intersection;
            float radius;
            if (Triangles != null)
                foreach (Triangle t in Triangles)
                {
                    r1 = GetRectEcuation(t.a, t.b);
                    r2 = GetMediatrixEcuation(t.a, t.b, r1);
                    r3 = GetRectEcuation(t.b, t.c);
                    r4 = GetMediatrixEcuation(t.b, t.c, r3);
                    intersection = GetIntersectPoint(r2, r4);
                    radius = (float)GetDistance(t.a, intersection);
                    res[t] = new Circle(radius, (Point2D)intersection.Clone());
                }
            return res;
        }

        public float GetDistance(Point2D p1, Point2D p2)
        {
            return (float)Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));
        }

        public Point2D GetIntersectPoint(Rect r1, Rect r2)
        {
            float x = (r2.N - r1.N) / (float)(r1.PendientEval() - r2.PendientEval());
            float y = r1.Eval(x);
            return new Point2D(x, y);
        }

        public Rect GetMediatrixEcuation(Point2D p1, Point2D p2, Rect r)
        {
            Point2D halfPoint = new Point2D((p1.x + p2.x) / 2f, (p1.y + p2.y) / 2f);
            Rational m = new Rational(r.M.denominator, r.M.numerator * -1);
            return new Rect(m, -1 * m.Eval() * halfPoint.x + halfPoint.y);
        }

        public Rect GetRectEcuation(Point2D p1, Point2D p2)
        {
            return new Rect(new Rational((int)(p1.y - p2.y), (int)(p1.x - p2.x)), -1 * p1.x * ((float)(p1.y - p2.y) / (float)(p1.x - p2.x)) + p1.y);
        }

    }

    public class Rect
    {
        public Rational M;  // наклон прямой
        public float N; // значение y, когда эта прямая пересекает ось y
        public Rect() { }
        public Rect(Rational M, float N)
        {
            this.M = M; this.N = N;
        }

        public float PendientEval()
        {
            return M.Eval();
        }

        public float Eval(float x)
        {
            return x * PendientEval() + N;
        }

    }

    public class Rational
    {
        public int numerator, denominator;
        public Rational(int numerator, int denominator)
        {
            this.numerator = numerator; this.denominator = denominator;
        }

        public float Eval()
        {
            if (denominator == 0) return 0;
            return (float)numerator / (float)denominator;
        }
    }

    public class Circle
    {
        public float radius = 0;
        public Point2D center = new Point2D();

        public Circle() { }
        public Circle(float radius, Point2D center)
        {
            this.radius = radius;
            this.center = center;
        }

    }

    public class Point2D
    {
        public float x, y;

        public Point2D() { }

        public Point2D(float x, float y)
        {
            this.x = x; this.y = y;
        }

        public override bool Equals(object obj)
        {
            Point2D other = (Point2D)obj;
            return other.x == x && other.y == y;
        }

        public object Clone()
        {
            return new Point2D(x, y);
        }


    }

    public class Triangle
    {
        public Point2D a, b, c;

        bool FindAssign(Point2D a1, Point2D a2, Point2D a3, float low)
        {
            if (a1.x == low)
            {
                if (a2.x == low)
                {
                    if (a1.y > a2.y)
                    {
                        this.a = a1;
                        this.b = a2;
                        this.c = a3;
                        return true;
                    }
                    else { this.a = a2; this.b = a1; this.c = a3; return true; }
                }
                else if (a3.x == low)
                {
                    if (a1.y > a3.y)
                    {
                        this.a = a1;
                        this.b = a3;
                        this.c = a2;
                        return true;
                    }
                    else { this.a = a3; this.b = a1; this.c = a2; return true; }
                }
                else if (a2.x > a3.x)
                { this.a = a3; this.b = a1; this.c = a2; return true; }
                else { this.a = a2; this.b = a1; this.c = a3; }
            }
            return false;
        }

        public Triangle(Point2D a, Point2D b, Point2D c)
        {
            if (a.Equals(b) || a.Equals(c) || b.Equals(c))
                throw new Exception("Points cannot be equal!");
            float low = Math.Min(a.x, b.x);
            low = Math.Min(low, c.x);
            if (!FindAssign(a, b, c, low) && !FindAssign(b, a, c, low))
                FindAssign(c, a, b, low);
        }

        public Triangle() { }

        public override bool Equals(object obj)
        {
            Triangle other = obj as Triangle;
            return other.a.Equals(a) && other.b.Equals(b) && other.c.Equals(c);
        }
    }

    class Pair
    {
        public List<Triangle> res;
        public bool ok = false;
        public Pair(List<Triangle> res, bool ok)
        {
            this.res = res; this.ok = ok;
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using Lab9;

namespace Lab9
{
    public partial class MainForm : Form
    {
        private Polyhedron currentPolyhedron;
        private string projectionType = "Perspective";
        private const double ROTATION_ANGLE_STEP = 0.01;

        private double totalRotationX = 0;
        private double totalRotationY = 0;

        private Vector3 viewVector = new Vector3(0, 0, 1);

        private Vector3 lightPosition = new Vector3(5, 5, 5);
        private Vector3 ambientColor = new Vector3(0.1, 0.1, 0.1);
        private Vector3 diffuseColor = new Vector3(0.7, 0.7, 0.7);
        private Vector3 specularColor = new Vector3(0.9, 0.9, 0.9);
        private double shininess = 32.0;

        private double scaleFactor = 1.0;
        private Vector3 translation = new Vector3(0, 0, 0);

        public MainForm()
        {
            InitializeComponent();
            currentPolyhedron = Polyhedron.CreateTetrahedron();
            this.DoubleBuffered = true;

            Timer rotationTimer = new Timer();
            rotationTimer.Interval = 30;
            rotationTimer.Tick += RotationTimer_Tick;
            rotationTimer.Start();

            perspectiveToolStripMenuItem.Checked = true;
            axonometricToolStripMenuItem.Checked = false;
        }

        private void RotationTimer_Tick(object sender, EventArgs e)
        {
            totalRotationX += ROTATION_ANGLE_STEP;
            totalRotationY += ROTATION_ANGLE_STEP;
            this.Invalidate();
        }

        private Vector3 RotateX(Vector3 v, double angle)
        {
            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);
            return new Vector3(v.X, v.Y * cos - v.Z * sin, v.Y * sin + v.Z * cos);
        }

        private Vector3 RotateY(Vector3 v, double angle)
        {
            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);
            return new Vector3(v.X * cos + v.Z * sin, v.Y, -v.X * sin + v.Z * cos);
        }

        private Vector3 Scale(Vector3 v, double s)
        {
            return new Vector3(v.X * s, v.Y * s, v.Z * s);
        }

        private Vector3 Translate(Vector3 v, Vector3 t)
        {
            return v + t;
        }

        private PointF ProjectPerspective(Vector3 v, int width, int height, double scale)
        {
            double distance = 5.0;
            double z_prime = distance / (distance - v.Z);
            float x = (float)(v.X * z_prime * scale + width / 2);
            float y = (float)(-v.Y * z_prime * scale + height / 2);
            return new PointF(x, y);
        }

        private PointF ProjectAxonometric(Vector3 v, int width, int height, double scale)
        {
            float x = (float)(v.X * scale + width / 2);
            float y = (float)(-v.Y * scale + height / 2);
            return new PointF(x, y);
        }

        private Color CalculatePhongColor(Vector3 vertex, Vector3 normal, Vector3 viewPos)
        {
            Vector3 objectColor = new Vector3(0.0, 0.5, 0.8);

            Vector3 lightDir = (lightPosition - vertex).Normalize();

            double diff = Math.Max(normal.Dot(lightDir), 0.0);
            Vector3 diffuse = objectColor * diffuseColor * diff;

            Vector3 viewDir = (viewPos - vertex).Normalize();
            Vector3 reflectDir = (2 * normal.Dot(lightDir) * normal - lightDir).Normalize();

            double spec = 0.0;
            if (diff > 0.0)
            {
                spec = Math.Pow(Math.Max(viewDir.Dot(reflectDir), 0.0), shininess);
            }
            Vector3 specular = specularColor * spec;

            Vector3 ambient = objectColor * ambientColor;

            Vector3 finalColor = ambient + diffuse + specular;

            int r = (int)(Math.Min(1.0, finalColor.X) * 255);
            int g = (int)(Math.Min(1.0, finalColor.Y) * 255);
            int b = (int)(Math.Min(1.0, finalColor.Z) * 255);

            return Color.FromArgb(r, g, b);
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.Black);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (currentPolyhedron == null) return;

            int width = ClientSize.Width;
            int height = ClientSize.Height;
            double scale = Math.Min(width, height) / 5;

            Vector3[] transformedVertices = new Vector3[currentPolyhedron.Vertices.Count];
            for (int i = 0; i < currentPolyhedron.Vertices.Count; i++)
            {
                Vector3 v = currentPolyhedron.Vertices[i];

                v = Scale(v, scaleFactor);
                v = RotateX(v, totalRotationX);
                v = RotateY(v, totalRotationY);
                v = Translate(v, translation);

                transformedVertices[i] = v;
            }

            var sortedPolygons = currentPolyhedron.Polygons
                .Select((p, index) => new { Polygon = p, Index = index })
                .OrderByDescending(item =>
                {
                    double avgZ = 0;
                    foreach (int idx in item.Polygon.Indices)
                    {
                        avgZ += transformedVertices[idx].Z;
                    }
                    return avgZ / item.Polygon.Indices.Length;
                }).ToList();

            Vector3 cameraPosition = new Vector3(0, 0, 5);

            foreach (var item in sortedPolygons)
            {
                var polygon = item.Polygon;
                int normalIndex = item.Index;

                Vector3 faceNormal = currentPolyhedron.FaceNormals[normalIndex];
                faceNormal = RotateX(faceNormal, totalRotationX);
                faceNormal = RotateY(faceNormal, totalRotationY);

                bool isFrontFacing = false;

                if (projectionType == "Perspective")
                {
                    Vector3 faceVertex = transformedVertices[polygon.Indices[0]];
                    Vector3 viewRay = cameraPosition - faceVertex;
                    isFrontFacing = faceNormal.Dot(viewRay) > 0;
                }
                else
                {
                    isFrontFacing = faceNormal.Dot(viewVector) > 0;
                }

                if (!isFrontFacing)
                {
                    continue;
                }

                Vector3 centerVertex = new Vector3(0, 0, 0);
                foreach (int idx in polygon.Indices)
                {
                    centerVertex += transformedVertices[idx];
                }
                centerVertex *= (1.0 / polygon.Indices.Length);

                Color fillColor = CalculatePhongColor(centerVertex, faceNormal, cameraPosition);

                PointF[] points = new PointF[polygon.Indices.Length];
                for (int j = 0; j < polygon.Indices.Length; j++)
                {
                    Vector3 v = transformedVertices[polygon.Indices[j]];

                    PointF p;
                    if (projectionType == "Perspective")
                    {
                        p = ProjectPerspective(v, width, height, scale);
                    }
                    else
                    {
                        p = ProjectAxonometric(v, width, height, scale);
                    }
                    points[j] = p;
                }

                using (Brush brush = new SolidBrush(fillColor))
                {
                    g.FillPolygon(brush, points);
                }
                g.DrawPolygon(Pens.White, points);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            double step = 0.1;
            double rotationStep = 0.1;

            if (keyData == Keys.Add || keyData == Keys.Oemplus)
            {
                scaleFactor += step;
                this.Invalidate();
                return true;
            }
            else if (keyData == Keys.Subtract || keyData == Keys.OemMinus)
            {
                scaleFactor = Math.Max(0.1, scaleFactor - step);
                this.Invalidate();
                return true;
            }
            else if (keyData == Keys.W) { translation.Y += step; this.Invalidate(); return true; }
            else if (keyData == Keys.S) { translation.Y -= step; this.Invalidate(); return true; }
            else if (keyData == Keys.A) { translation.X -= step; this.Invalidate(); return true; }
            else if (keyData == Keys.D) { translation.X += step; this.Invalidate(); return true; }
            else if (keyData == Keys.Q) { translation.Z += step; this.Invalidate(); return true; }
            else if (keyData == Keys.E) { translation.Z -= step; this.Invalidate(); return true; }

            else if (keyData == Keys.Left)
            {
                totalRotationY -= rotationStep;
                this.Invalidate();
                return true;
            }
            else if (keyData == Keys.Right)
            {
                totalRotationY += rotationStep;
                this.Invalidate();
                return true;
            }
            else if (keyData == Keys.Up)
            {
                totalRotationX -= rotationStep;
                this.Invalidate();
                return true;
            }
            else if (keyData == Keys.Down)
            {
                totalRotationX += rotationStep;
                this.Invalidate();
                return true;
            }

            if (projectionType == "Axonometric")
            {
                Vector3 rotationAxis = new Vector3(0, 0, 0);

                if (keyData == Keys.NumPad4) rotationAxis = new Vector3(0, 1, 0);
                else if (keyData == Keys.NumPad6) rotationAxis = new Vector3(0, -1, 0);
                else if (keyData == Keys.NumPad8) rotationAxis = new Vector3(-1, 0, 0);
                else if (keyData == Keys.NumPad2) rotationAxis = new Vector3(1, 0, 0);
                else if (keyData == Keys.NumPad7) rotationAxis = new Vector3(0, 0, 1);
                else if (keyData == Keys.NumPad9) rotationAxis = new Vector3(0, 0, -1);

                if (rotationAxis.Length > 0)
                {
                    double angle = Math.PI / 18;
                    double cos = Math.Cos(angle);
                    double sin = Math.Sin(angle);

                    if (rotationAxis.Y != 0)
                    {
                        double x = viewVector.X;
                        double z = viewVector.Z;
                        viewVector.X = x * cos - z * sin * rotationAxis.Y;
                        viewVector.Z = x * sin * rotationAxis.Y + z * cos;
                    }
                    if (rotationAxis.X != 0)
                    {
                        double y = viewVector.Y;
                        double z = viewVector.Z;
                        viewVector.Y = y * cos - z * sin * rotationAxis.X;
                        viewVector.Z = y * sin * rotationAxis.X + z * cos;
                    }

                    viewVector = viewVector.Normalize();
                    this.Invalidate();
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void tetrahedronToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentPolyhedron = Polyhedron.CreateTetrahedron();
            this.Invalidate();
        }

        private void hexahedronToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentPolyhedron = Polyhedron.CreateHexahedron();
            this.Invalidate();
        }

        private void octahedronToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentPolyhedron = Polyhedron.CreateOctahedron();
            this.Invalidate();
        }

        private void icosahedronToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentPolyhedron = Polyhedron.CreateIcosahedron();
            this.Invalidate();
        }

        private void dodecahedronToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentPolyhedron = Polyhedron.CreateDodecahedron();
            this.Invalidate();
        }

        private void perspectiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            projectionType = "Perspective";
            perspectiveToolStripMenuItem.Checked = true;
            axonometricToolStripMenuItem.Checked = false;
            this.Invalidate();
        }

        private void axonometricToolStripMenuItem_Click(object sender, EventArgs e)
        {
            projectionType = "Axonometric";
            perspectiveToolStripMenuItem.Checked = false;
            axonometricToolStripMenuItem.Checked = true;
            this.Invalidate();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "OBJ Files (*.obj)|*.obj|All Files (*.*)|*.*";
                openFileDialog.Title = "Open Polyhedron Model (OBJ)";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        currentPolyhedron = ObjFileHandler.LoadFromObj(openFileDialog.FileName);
                        this.Invalidate();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при загрузке модели: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentPolyhedron == null)
            {
                MessageBox.Show("Нет модели многогранника для сохранения.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "OBJ Files (*.obj)|*.obj|All Files (*.*)|*.*";
                saveFileDialog.Title = "Save Polyhedron Model (OBJ)";
                saveFileDialog.FileName = "polyhedron.obj";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ObjFileHandler.SaveToObj(currentPolyhedron, saveFileDialog.FileName);
                        MessageBox.Show("Модель успешно сохранена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при сохранении модели: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
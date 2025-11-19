using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using Lab9;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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

        private Bitmap texture;
        private Bitmap frameBitmap;

        private byte[] textureBuffer;
        private int textureStride;

        private byte[] frameBuffer;
        private int stride;
        private readonly int bytesPerPixel = 4;

        private bool useTexture = false;

        public MainForm()
        {
            InitializeComponent();
            currentPolyhedron = Polyhedron.CreateTetrahedron();
            this.DoubleBuffered = true;

            Timer rotationTimer = new Timer();
            rotationTimer.Interval = 30;
            rotationTimer.Tick += RotationTimer_Tick;
            rotationTimer.Start();

            ToolStripMenuItem loadTextureMenuItem = new ToolStripMenuItem("Load Texture");
            loadTextureMenuItem.Click += loadTextureToolStripMenuItem_Click;
            fileToolStripMenuItem.DropDownItems.Add(loadTextureMenuItem);

            perspectiveToolStripMenuItem.Checked = true;
            axonometricToolStripMenuItem.Checked = false;

            ToolStripMenuItem toggleTextureToolStripMenuItem = new ToolStripMenuItem("Включить текстуру");
            toggleTextureToolStripMenuItem.Checked = useTexture;
            toggleTextureToolStripMenuItem.Click += (s, e) =>
            {
                useTexture = !useTexture;
                toggleTextureToolStripMenuItem.Checked = useTexture;
                this.Invalidate();
            };
            menuStrip1.Items.Add(toggleTextureToolStripMenuItem);

            texture = CreateCheckerboardTexture(256, 256, 16);
            CacheTexture(texture);
        }
        private void toggleTextureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            useTexture = !useTexture;
            ((ToolStripMenuItem)sender).Checked = useTexture;
            this.Invalidate();
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

            if (currentPolyhedron == null) return;

            int width = Math.Max(1, ClientSize.Width);
            int height = Math.Max(1, ClientSize.Height);
            double scale = Math.Min(width, height) / 5.0;

            if (frameBitmap == null || frameBitmap.Width != width || frameBitmap.Height != height)
            {
                if (frameBitmap != null) frameBitmap.Dispose();
                frameBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }

            Rectangle rect = new Rectangle(0, 0, width, height);
            var bmpData = frameBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, frameBitmap.PixelFormat);
            stride = Math.Abs(bmpData.Stride);
            int bufferSize = stride * height;
            if (frameBuffer == null || frameBuffer.Length != bufferSize)
                frameBuffer = new byte[bufferSize];

            for (int i = 0; i < bufferSize; i += 4)
            {
                frameBuffer[i + 0] = 0;
                frameBuffer[i + 1] = 0;
                frameBuffer[i + 2] = 0;
                frameBuffer[i + 3] = 255;
            }

            double[,] zBuffer = new double[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    zBuffer[x, y] = double.NegativeInfinity;

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
                        avgZ += transformedVertices[idx].Z;
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
                if (!isFrontFacing) continue;

                Vector3 centerVertex = new Vector3(0, 0, 0);
                foreach (int idx in polygon.Indices)
                    centerVertex += transformedVertices[idx];
                centerVertex *= (1.0 / polygon.Indices.Length);

                Color fillColor = CalculatePhongColor(centerVertex, faceNormal, cameraPosition);

                int n = polygon.Indices.Length;
                PointF[] pts2D = new PointF[n];
                double[] zs = new double[n];
                Vector2UV[] uvs = new Vector2UV[n];

                Vector2UV[] triUV = new Vector2UV[] { new Vector2UV(0, 0), new Vector2UV(1, 0), new Vector2UV(0.5, 1) };
                Vector2UV[] quadUV = new Vector2UV[] { new Vector2UV(0, 0), new Vector2UV(1, 0), new Vector2UV(1, 1), new Vector2UV(0, 1) };

                for (int j = 0; j < n; j++)
                {
                    Vector3 v = transformedVertices[polygon.Indices[j]];
                    pts2D[j] = projectionType == "Perspective" ? ProjectPerspective(v, width, height, scale)
                                                               : ProjectAxonometric(v, width, height, scale);
                    zs[j] = v.Z;

                    if (n == 3) uvs[j] = triUV[j];
                    else if (n == 4) uvs[j] = quadUV[j];
                    else
                    {
                        double ang = (double)j / n * Math.PI * 2.0;
                        uvs[j] = new Vector2UV(0.5 + 0.5 * Math.Cos(ang), 0.5 + 0.5 * Math.Sin(ang));
                    }
                }

                for (int t = 1; t < n - 1; t++)
                {
                    PointF A = pts2D[0], B = pts2D[t], C = pts2D[t + 1];
                    double zA = zs[0], zB = zs[t], zC = zs[t + 1];
                    Vector2UV uvA = uvs[0], uvB = uvs[t], uvC = uvs[t + 1];

                    int minX = (int)Math.Max(0, Math.Floor(Math.Min(A.X, Math.Min(B.X, C.X))));
                    int maxX = (int)Math.Min(width - 1, Math.Ceiling(Math.Max(A.X, Math.Max(B.X, C.X))));
                    int minY = (int)Math.Max(0, Math.Floor(Math.Min(A.Y, Math.Min(B.Y, C.Y))));
                    int maxY = (int)Math.Min(height - 1, Math.Ceiling(Math.Max(A.Y, Math.Max(B.Y, C.Y))));

                    for (int py = minY; py <= maxY; py++)
                    {
                        for (int px = minX; px <= maxX; px++)
                        {
                            PointF P = new PointF(px + 0.5f, py + 0.5f);
                            Barycentric(P, A, B, C, out double w1, out double w2, out double w3);
                            if (w1 < -1e-6 || w2 < -1e-6 || w3 < -1e-6) continue;

                            double z = w1 * zA + w2 * zB + w3 * zC;
                            if (z <= zBuffer[px, py]) continue;

                            double u = w1 * uvA.U + w2 * uvB.U + w3 * uvC.U;
                            double v = w1 * uvA.V + w2 * uvB.V + w3 * uvC.V;

                            Color texColor = Color.White;
                            if (useTexture && texture != null)
                            {
                                texColor = SampleTexture(texture, u, v);
                            }

                            int rr = texColor.R * fillColor.R / 255;
                            int gg = texColor.G * fillColor.G / 255;
                            int bb = texColor.B * fillColor.B / 255;

                            FastSetPixel(frameBuffer, stride, px, py, Color.FromArgb(Clamp(rr), Clamp(gg), Clamp(bb)));
                            zBuffer[px, py] = z;
                        }
                    }
                }
            }


            Marshal.Copy(frameBuffer, 0, bmpData.Scan0, frameBuffer.Length);
            frameBitmap.UnlockBits(bmpData);
            g.DrawImage(frameBitmap, 0, 0, width, height);

        }

        private Bitmap CreateCheckerboardTexture(int width, int height, int cell)
        {
            Bitmap bmp = new Bitmap(width, height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int cx = x / cell;
                    int cy = y / cell;
                    bool black = ((cx + cy) % 2) == 0;
                    bmp.SetPixel(x, y, black ? Color.DarkSlateBlue : Color.LightGray);
                }
            return bmp;
        }

        private Color SampleTexture(Bitmap tex, double u, double v)
        {
            if (textureBuffer == null || tex == null) return Color.White;

            u = u % 1.0;
            if (u < 0) u += 1.0;
            v = v % 1.0;
            if (v < 0) v += 1.0;

            int x = (int)(u * (tex.Width - 1));
            int y = (int)((1.0 - v) * (tex.Height - 1));
            x = Math.Max(0, Math.Min(tex.Width - 1, x));
            y = Math.Max(0, Math.Min(tex.Height - 1, y));

            int idx = y * textureStride + x * 4;
            byte b = textureBuffer[idx + 0];
            byte g = textureBuffer[idx + 1];
            byte r = textureBuffer[idx + 2];

            return Color.FromArgb(r, g, b);
        }


        private void Barycentric(PointF p, PointF a, PointF b, PointF c, out double u, out double v, out double w)
        {
            double denom = (b.Y - c.Y) * (a.X - c.X) + (c.X - b.X) * (a.Y - c.Y);
            if (Math.Abs(denom) < 1e-9)
            {
                u = v = w = -1;
                return;
            }
            u = ((b.Y - c.Y) * (p.X - c.X) + (c.X - b.X) * (p.Y - c.Y)) / denom;
            v = ((c.Y - a.Y) * (p.X - c.X) + (a.X - c.X) * (p.Y - c.Y)) / denom;
            w = 1.0 - u - v;
        }

        private int Clamp(int val)
        {
            if (val < 0) return 0;
            if (val > 255) return 255;
            return val;
        }

        private void FastSetPixel(byte[] buffer, int strideLocal, int x, int y, Color c)
        {
            if (x < 0 || x >= (strideLocal / bytesPerPixel) || y < 0) return;
            int index = y * strideLocal + x * bytesPerPixel;
            if (index + 2 >= buffer.Length || index < 0) return;
            buffer[index + 0] = c.B;
            buffer[index + 1] = c.G;
            buffer[index + 2] = c.R;
            buffer[index + 3] = 255;
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

        private void CacheTexture(Bitmap tex)
        {
            if (tex == null) return;

            BitmapData bmpData = tex.LockBits(new Rectangle(0, 0, tex.Width, tex.Height),
                                              ImageLockMode.ReadOnly,
                                              PixelFormat.Format32bppArgb);
            textureStride = bmpData.Stride;
            int size = textureStride * tex.Height;
            textureBuffer = new byte[size];
            Marshal.Copy(bmpData.Scan0, textureBuffer, 0, size);
            tex.UnlockBits(bmpData);
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

        private void loadTextureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp|All files (*.*)|*.*";
                ofd.Title = "Load Texture";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        Bitmap t = (Bitmap)Bitmap.FromFile(ofd.FileName);
                        if (t != null)
                        {
                            if (texture != null) texture.Dispose();
                            texture = new Bitmap(t);
                            t.Dispose();
                            CacheTexture(texture);
                            this.Invalidate();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при загрузке текстуры: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}

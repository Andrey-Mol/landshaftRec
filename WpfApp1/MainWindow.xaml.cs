using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf; // Добавьте ссылку на Helix Toolkit

namespace Fractal
{
    public partial class MainWindow : Window
    {
        private const double InitialSize = 10; // Размер начального квадрата
        private MeshGeometry3D landscapeMesh;
        private GeometryModel3D landscapeModel;
        public int depth = 4;
        public double randomness = 0.8;
        private bool drawWireframe = false; // Флаг для переключения режимов отрисовки

        public MainWindow()
        {
            InitializeComponent();
            GenerateLandscape(depth, randomness);
            this.Background = Brushes.Lavender;
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем глубину рекурсии и коэффициент случайности из интерфейса
                int.TryParse(DepthInput.Text, out int depth);
                RandomnessInput.Text = RandomnessInput.Text.Replace('.', ',');
                double.TryParse(RandomnessInput.Text, out double randomness);
                GenerateLandscape(depth, randomness);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private Color GetColorBasedOnHeight(double height)
        {
            if (height < 0.2) return Colors.DeepSkyBlue;       // Вода
            if (height < 0.3) return Colors.LightGoldenrodYellow; // Песок
            if (height < 0.6) return Colors.ForestGreen;       // Трава
            if (height < 0.8) return Colors.DarkGray;          // Горы
            return Colors.WhiteSmoke;                              // Снег
        }

        private void GenerateLandscape(int depth, double randomness)
        {
            // Создаем новый MeshGeometry3D
            landscapeMesh = new MeshGeometry3D();

            // Используем DiffuseMaterial для реагирования на свет
            var material = new DiffuseMaterial
            {
                Brush = new SolidColorBrush(Colors.Green) // Зеленый для земли
            };
            landscapeModel = new GeometryModel3D(landscapeMesh, material);

            var modelVisual = new ModelVisual3D { Content = landscapeModel };
            Viewport.Children.Clear();
            Viewport.Children.Add(modelVisual);

            // Генерация ландшафта
            var size = InitialSize;
            var heights = DiamondSquare(depth, randomness, size);
            SmoothLandscape(heights); // Сглаживание ландшафта
            BuildMesh(heights, size);

            // Центрируем ландшафт
            CenterLandscape(size);

            // Добавляем освещение
            var directionalLight = new DirectionalLight
            {
                Color = Colors.White,
                Direction = new Vector3D(-1, -1, -1) // Направление света
            };
            Viewport.Children.Add(new ModelVisual3D { Content = directionalLight });

            // Опционально: общий свет
            var ambientLight = new AmbientLight
            {
                Color = Colors.Gray // Тусклый общий свет
            };
            Viewport.Children.Add(new ModelVisual3D { Content = ambientLight });
        }

        private double[,] DiamondSquare(int depth, double randomness, double size)
        {
            int pointsPerSide = (int)Math.Pow(2, depth) + 1;
            double[,] heights = new double[pointsPerSide, pointsPerSide];
            var random = new Random();

            // Инициализация углов
            heights[0, 0] = random.NextDouble();
            heights[0, pointsPerSide - 1] = random.NextDouble();
            heights[pointsPerSide - 1, 0] = random.NextDouble();
            heights[pointsPerSide - 1, pointsPerSide - 1] = random.NextDouble();

            int step = pointsPerSide - 1;
            while (step > 1)
            {
                // Алгоритм "Diamond" шаг
                for (int x = 0; x < pointsPerSide - 1; x += step)
                {
                    for (int y = 0; y < pointsPerSide - 1; y += step)
                    {
                        double avg = (heights[x, y] +
                                      heights[x + step, y] +
                                      heights[x, y + step] +
                                      heights[x + step, y + step]) / 4.0;

                        heights[x + step / 2, y + step / 2] = avg + (random.NextDouble() * 2 - 1) * randomness * step / size;
                    }
                }

                // Алгоритм "Square" шаг
                for (int x = 0; x < pointsPerSide; x += step / 2)
                {
                    for (int y = (x + step / 2) % step; y < pointsPerSide; y += step)
                    {
                        double sum = 0;
                        int count = 0;

                        if (x >= step / 2) { sum += heights[x - step / 2, y]; count++; }
                        if (x + step / 2 < pointsPerSide) { sum += heights[x + step / 2, y]; count++; }
                        if (y >= step / 2) { sum += heights[x, y - step / 2]; count++; }
                        if (y + step / 2 < pointsPerSide) { sum += heights[x, y + step / 2]; count++; }

                        heights[x, y] = sum / count + (random.NextDouble() * 2 - 1) * randomness * step / size;
                    }
                }

                step /= 2;
                randomness /= 1.6; // Уменьшение случайности
            }

            return heights;
        }

        private void SmoothLandscape(double[,] heights)
        {
            int pointsPerSide = heights.GetLength(0);
            double[,] smoothedHeights = new double[pointsPerSide, pointsPerSide];

            for (int x = 0; x < pointsPerSide; x++)
            {
                for (int y = 0; y < pointsPerSide; y++)
                {
                    double sum = 0;
                    int count = 0;

                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int nx = x + i;
                            int ny = y + j;

                            if (nx >= 0 && nx < pointsPerSide && ny >= 0 && ny < pointsPerSide)
                            {
                                sum += heights[nx, ny];
                                count++;
                            }
                        }
                    }

                    smoothedHeights[x, y] = sum / count;
                }
            }

            for (int x = 0; x < pointsPerSide; x++)
            {
                for (int y = 0; y < pointsPerSide; y++)
                {
                    heights[x, y] = smoothedHeights[x, y];
                }
            }
        }

        private void BuildMesh(double[,] heights, double size)
        {
            int pointsPerSide = heights.GetLength(0);
            double step = size / (pointsPerSide - 1);

            var modelGroup = new Model3DGroup();

            // Построение вершин
            for (int x = 0; x < pointsPerSide - 1; x++)
            {
                for (int y = 0; y < pointsPerSide - 1; y++)
                {
                    // Определяем вершины треугольников
                    var p1 = new Point3D(x * step - size / 2, y * step - size / 2, heights[x, y]);
                    var p2 = new Point3D((x + 1) * step - size / 2, y * step - size / 2, heights[x + 1, y]);
                    var p3 = new Point3D(x * step - size / 2, (y + 1) * step - size / 2, heights[x, y + 1]);
                    var p4 = new Point3D((x + 1) * step - size / 2, (y + 1) * step - size / 2, heights[x + 1, y + 1]);

                    // Добавляем два треугольника
                    if (drawWireframe)
                    {
                        // Отрисовываем треугольники с белым заполнением и чёрными границами
                        AddWireframeTriangle(p1, p2, p3, modelGroup);
                        AddWireframeTriangle(p2, p4, p3, modelGroup);
                    }
                    else
                    {
                        // Отрисовываем заполненные треугольники с цветами
                        AddColoredTriangle(p1, p2, p3, modelGroup);
                        AddColoredTriangle(p2, p4, p3, modelGroup);
                    }
                }
            }

            // Устанавливаем модель на сцену
            landscapeModel = new GeometryModel3D
            {
                Geometry = landscapeMesh,
                Material = new DiffuseMaterial(new SolidColorBrush(Colors.Transparent))
            };
            Viewport.Children.Clear();
            Viewport.Children.Add(new ModelVisual3D { Content = modelGroup });

            // Отрисовываем границы с помощью Helix Toolkit
            if (drawWireframe)
            {
                var wireframe = new LinesVisual3D();
                wireframe.Color = Colors.Black;

                // Добавляем линии для всех треугольников
                for (int x = 0; x < pointsPerSide - 1; x++)
                {
                    for (int y = 0; y < pointsPerSide - 1; y++)
                    {
                        var p1 = new Point3D(x * step - size / 2, y * step - size / 2, heights[x, y]);
                        var p2 = new Point3D((x + 1) * step - size / 2, y * step - size / 2, heights[x + 1, y]);
                        var p3 = new Point3D(x * step - size / 2, (y + 1) * step - size / 2, heights[x, y + 1]);
                        var p4 = new Point3D((x + 1) * step - size / 2, (y + 1) * step - size / 2, heights[x + 1, y + 1]);

                        // Добавляем линии для каждого треугольника
                        wireframe.Points.Add(p1);
                        wireframe.Points.Add(p2);
                        wireframe.Points.Add(p2);
                        wireframe.Points.Add(p3);
                        wireframe.Points.Add(p3);
                        wireframe.Points.Add(p1);

                        wireframe.Points.Add(p2);
                        wireframe.Points.Add(p4);
                        wireframe.Points.Add(p4);
                        wireframe.Points.Add(p3);
                    }
                }

                Viewport.Children.Add(wireframe);
            }
        }

        private void AddColoredTriangle(Point3D p1, Point3D p2, Point3D p3, Model3DGroup modelGroup)
        {
            // Создаем треугольник
            var mesh = new MeshGeometry3D();
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            // Вычисляем среднюю высоту для выбора цвета
            var avgHeight = (p1.Z + p2.Z + p3.Z) / 3;
            var color = GetColorBasedOnHeight(avgHeight);

            // Применяем цвет
            var material = new DiffuseMaterial(new SolidColorBrush(color));
            modelGroup.Children.Add(new GeometryModel3D(mesh, material));
        }

        private void AddWireframeTriangle(Point3D p1, Point3D p2, Point3D p3, Model3DGroup modelGroup)
        {
            // Создаем треугольник
            var mesh = new MeshGeometry3D();
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            // Применяем белый материал для заполнения
            var material = new DiffuseMaterial(new SolidColorBrush(Colors.White));
            modelGroup.Children.Add(new GeometryModel3D(mesh, material));
        }

        private void CenterLandscape(double size)
        {
            Camera.Position = new Point3D(0, -size, size);
            Camera.LookDirection = new Vector3D(0, size, -size);
        }

        private void WireframeModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            drawWireframe = true; // Устанавливаем режим "Только границы"
            try
            {
                // Получаем глубину рекурсии и коэффициент случайности из интерфейса
                int.TryParse(DepthInput.Text, out int depth);
                RandomnessInput.Text = RandomnessInput.Text.Replace('.', ',');
                double.TryParse(RandomnessInput.Text, out double randomness);
                GenerateLandscape(depth, randomness);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void WireframeModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            drawWireframe = false; // Устанавливаем режим "Заполнение цветами"
            try
            {
                // Получаем глубину рекурсии и коэффициент случайности из интерфейса
                int.TryParse(DepthInput.Text, out int depth);
                RandomnessInput.Text = RandomnessInput.Text.Replace('.', ',');
                double.TryParse(RandomnessInput.Text, out double randomness);
                GenerateLandscape(depth, randomness);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
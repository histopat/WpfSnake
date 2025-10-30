using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfSnake
{
    public partial class MainWindow : Window
    {
        private const int GridSize = 20;
        private const int CellSize = 20;
        private const int InitialSpeed = 10;
        private const int SpeedIncrement = 2;
        private const int MaxSpeed = 30;

        private DispatcherTimer gameTimer;
        private List<Point> snake;
        private Point food;
        private Direction currentDirection;
        private Direction nextDirection;
        private int score;
        private int speed;
        private bool isGameOver;
        private Random random;
        private ResourceManager resourceManager;

        public MainWindow()
        {
            InitializeComponent();
            resourceManager = new ResourceManager();
            random = new Random();
            InitializeGame();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Root.Focus();
        }

        private void InitializeGame()
        {
            snake = new List<Point>
            {
                new Point(10, 10),
                new Point(9, 10),
                new Point(8, 10)
            };

            currentDirection = Direction.Right;
            nextDirection = Direction.Right;
            score = 0;
            speed = InitialSpeed;
            isGameOver = false;

            PlaceFood();
            UpdateUI();

            if (gameTimer == null)
            {
                gameTimer = new DispatcherTimer();
                gameTimer.Tick += GameTimer_Tick;
            }

            gameTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / speed);
            gameTimer.Start();

            GameOverPanel.Visibility = Visibility.Collapsed;
            DrawGame();
        }

        private void PlaceFood()
        {
            do
            {
                food = new Point(random.Next(0, GridSize), random.Next(0, GridSize));
            }
            while (snake.Contains(food));
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (isGameOver) return;

            currentDirection = nextDirection;
            Point head = snake[0];
            Point newHead = GetNewHead(head);

            if (IsCollision(newHead))
            {
                GameOver();
                return;
            }

            snake.Insert(0, newHead);

            if (newHead == food)
            {
                score++;
                PlaceFood();

                if (score % 5 == 0 && speed < MaxSpeed)
                {
                    speed += SpeedIncrement;
                    gameTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / speed);
                }

                UpdateUI();
            }
            else
            {
                snake.RemoveAt(snake.Count - 1);
            }

            DrawGame();
        }

        private Point GetNewHead(Point head)
        {
            return currentDirection switch
            {
                Direction.Up => new Point(head.X, head.Y - 1),
                Direction.Down => new Point(head.X, head.Y + 1),
                Direction.Left => new Point(head.X - 1, head.Y),
                Direction.Right => new Point(head.X + 1, head.Y),
                _ => head
            };
        }

        private bool IsCollision(Point head)
        {
            return head.X < 0 || head.X >= GridSize ||
                   head.Y < 0 || head.Y >= GridSize ||
                   snake.Contains(head);
        }

        private void GameOver()
        {
            isGameOver = true;
            gameTimer.Stop();
            GameOverPanel.Visibility = Visibility.Visible;
            GameOverText.Text = resourceManager.GetString("GameOver");
            RestartButton.Content = resourceManager.GetString("Restart");
        }

        private void DrawGame()
        {
            GameCanvas.Children.Clear();

            // Draw food
            var foodRect = new Rectangle
            {
                Width = CellSize - 2,
                Height = CellSize - 2,
                Fill = (SolidColorBrush)FindResource("FoodBrush"),
                RadiusX = 4,
                RadiusY = 4
            };
            Canvas.SetLeft(foodRect, food.X * CellSize + 1);
            Canvas.SetTop(foodRect, food.Y * CellSize + 1);
            GameCanvas.Children.Add(foodRect);

            // Draw snake
            for (int i = 0; i < snake.Count; i++)
            {
                var segment = snake[i];
                var snakeRect = new Rectangle
                {
                    Width = CellSize - 2,
                    Height = CellSize - 2,
                    Fill = i == 0 ?
                        (SolidColorBrush)FindResource("SnakeHeadBrush") :
                        (SolidColorBrush)FindResource("SnakeBodyBrush"),
                    RadiusX = 3,
                    RadiusY = 3
                };
                Canvas.SetLeft(snakeRect, segment.X * CellSize + 1);
                Canvas.SetTop(snakeRect, segment.Y * CellSize + 1);
                GameCanvas.Children.Add(snakeRect);
            }
        }

        private void UpdateUI()
        {
            ScoreText.Text = string.Format(resourceManager.GetString("Score"), score);
            SpeedText.Text = string.Format(resourceManager.GetString("Speed"), speed);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (isGameOver)
            {
                if (e.Key == Key.Enter)
                {
                    InitializeGame();
                }
                return;
            }

            Direction newDirection = e.Key switch
            {
                Key.Up or Key.W => Direction.Up,
                Key.Down or Key.S => Direction.Down,
                Key.Left or Key.A => Direction.Left,
                Key.Right or Key.D => Direction.Right,
                _ => currentDirection
            };

            if (IsValidDirectionChange(newDirection))
            {
                nextDirection = newDirection;
            }
        }

        private bool IsValidDirectionChange(Direction newDirection)
        {
            return (currentDirection, newDirection) switch
            {
                (Direction.Up, Direction.Down) => false,
                (Direction.Down, Direction.Up) => false,
                (Direction.Left, Direction.Right) => false,
                (Direction.Right, Direction.Left) => false,
                _ => true
            };
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            InitializeGame();
            Root.Focus();
        }

        protected override void OnClosed(EventArgs e)
        {
            gameTimer?.Stop();
            base.OnClosed(e);
        }
    }

    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    public class ResourceManager
    {
        private readonly Dictionary<string, string> resources;

        public ResourceManager()
        {
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            resources = culture == "tr" ? GetTurkishResources() : GetEnglishResources();
        }

        private Dictionary<string, string> GetEnglishResources()
        {
            return new Dictionary<string, string>
            {
                { "Score", "Score: {0}" },
                { "Speed", "Speed: {0} fps" },
                { "GameOver", "Game Over!" },
                { "Restart", "Restart" },
                { "AppTitle", "WPF Snake" }
            };
        }

        private Dictionary<string, string> GetTurkishResources()
        {
            return new Dictionary<string, string>
            {
                { "Score", "Skor: {0}" },
                { "Speed", "Hız: {0} fps" },
                { "GameOver", "Oyun Bitti!" },
                { "Restart", "Yeniden Başlat" },
                { "AppTitle", "WPF Yılan" }
            };
        }

        public string GetString(string key)
        {
            return resources.TryGetValue(key, out var value) ? value : key;
        }
    }
}
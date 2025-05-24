using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace A_quickly_made_game
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer gameTimer;
        private bool isLeftPressed, isRightPressed, isSpacePressed;
        private List<Ellipse> bullets = new List<Ellipse>();
        private List<Rectangle> asteroids = new List<Rectangle>();
        private Random random = new Random();
        private int score = 0;
        private int level = 1;
        private int asteroidSpeed = 5;
        private bool gameOver = false;
        private DateTime lastShotTime = DateTime.Now; 
        private Path playerShip;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            FocusManager.SetFocusedElement(this, this);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitGame();
            Focus();
        }

        private void InitGame()
        {
            var elementsToRemove = new List<UIElement>();
            foreach (UIElement element in gameCanvas.Children)
            {
                if (element is Ellipse || element is Rectangle || element is Path)
                {
                    elementsToRemove.Add(element);
                }
            }
            foreach (var element in elementsToRemove)
            {
                gameCanvas.Children.Remove(element);
            }

            bullets.Clear();
            asteroids.Clear();

            playerShip = new Path
            {
                Fill = Brushes.Red,
                Stroke = Brushes.White,
                StrokeThickness = 1,
                Data = Geometry.Parse("M15,0 L0,30 L30,30 Z"),
                Width = 30,
                Height = 30
            };

            Canvas.SetLeft(playerShip, gameCanvas.ActualWidth / 2 - 15);
            Canvas.SetTop(playerShip, gameCanvas.ActualHeight - 60);
            gameCanvas.Children.Add(playerShip);

            score = 0;
            level = 1;
            asteroidSpeed = 5;
            gameOver = false;
            lastShotTime = DateTime.Now; 

            if (gameTimer != null)
            {
                gameTimer.Stop();
                gameTimer.Tick -= GameLoop;
            }

            gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) 
            };
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            this.Focus();
            Keyboard.Focus(this);

            UpdateUI();
            gameOverText.Visibility = Visibility.Collapsed;
            restartButton.Visibility = Visibility.Collapsed;
        }

        private void GameLoop(object sender, EventArgs e)
        {
            if (gameOver) return;

            double shipX = Canvas.GetLeft(playerShip);
            double shipWidth = playerShip.ActualWidth;

            if (isLeftPressed && shipX > 0) shipX -= 8;
            if (isRightPressed && shipX < gameCanvas.ActualWidth - shipWidth) shipX += 8;

            Canvas.SetLeft(playerShip, shipX);

            if (isSpacePressed && (DateTime.Now - lastShotTime).TotalMilliseconds > 300)
            {
                Shoot();
                lastShotTime = DateTime.Now;
            }

            MoveBullets();
            SpawnAsteroids();
            MoveAsteroids();

            if (score >= level * 100)
            {
                level++;
                asteroidSpeed += 2;
            }

            UpdateUI();
        }

        private void MoveBullets()
        {
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                var bullet = bullets[i];

                if (bullet.Parent == null || !gameCanvas.Children.Contains(bullet))
                {
                    bullets.RemoveAt(i);
                    continue;
                }

                double currentTop = Canvas.GetTop(bullet);
                double newTop = currentTop - 10;

                if (newTop < -bullet.ActualHeight)
                {
                    gameCanvas.Children.Remove(bullet);
                    bullets.RemoveAt(i);
                }
                else
                {
                    Canvas.SetTop(bullet, newTop);
                    CheckBulletCollisions(i, bullet);
                }
            }
        }

        private void CheckBulletCollisions(int bulletIndex, Ellipse bullet)
        {
            for (int j = asteroids.Count - 1; j >= 0; j--)
            {
                var asteroid = asteroids[j];
                if (CheckCollision(bullet, asteroid))
                {
                    gameCanvas.Children.Remove(bullet);
                    bullets.RemoveAt(bulletIndex);
                    gameCanvas.Children.Remove(asteroid);
                    asteroids.RemoveAt(j);
                    score += 10;
                    break;
                }
            }
        }

        private void SpawnAsteroids()
        {
            if (random.Next(0, 100) < 3)
            {
                CreateAsteroid();
            }
        }

        private void MoveAsteroids()
        {
            for (int i = asteroids.Count - 1; i >= 0; i--)
            {
                var asteroid = asteroids[i];
                double asteroidY = Canvas.GetTop(asteroid) + asteroidSpeed;

                if (asteroidY > gameCanvas.ActualHeight)
                {
                    gameCanvas.Children.Remove(asteroid);
                    asteroids.RemoveAt(i);
                }
                else
                {
                    Canvas.SetTop(asteroid, asteroidY);

                    if (CheckCollision(asteroid, playerShip))
                    {
                        GameOver();
                        return;
                    }
                }
            }
        }

        private void Shoot()
        {
            if (gameOver) return;

            Ellipse bullet = new Ellipse
            {
                Width = 8,
                Height = 20,
                Fill = Brushes.Yellow,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };

            double shipLeft = Canvas.GetLeft(playerShip);
            double shipTop = Canvas.GetTop(playerShip);
            double bulletX = shipLeft + playerShip.ActualWidth / 2 - bullet.Width / 2;
            double bulletY = shipTop - bullet.Height;

            Canvas.SetLeft(bullet, bulletX);
            Canvas.SetTop(bullet, bulletY);

            gameCanvas.Children.Add(bullet);
            bullets.Add(bullet);
        }

        private void CreateAsteroid()
        {
            int size = random.Next(20, 50);
            Rectangle asteroid = new Rectangle
            {
                Width = size,
                Height = size,
                Fill = Brushes.Gray,
                RadiusX = 10,
                RadiusY = 10
            };

            double asteroidX = random.Next(0, (int)(gameCanvas.ActualWidth - asteroid.Width));
            double asteroidY = -asteroid.Height;

            Canvas.SetLeft(asteroid, asteroidX);
            Canvas.SetTop(asteroid, asteroidY);

            gameCanvas.Children.Add(asteroid);
            asteroids.Add(asteroid);
        }

        private bool CheckCollision(FrameworkElement element1, FrameworkElement element2)
        {
            Rect rect1 = new Rect(
                Canvas.GetLeft(element1),
                Canvas.GetTop(element1),
                element1.ActualWidth,
                element1.ActualHeight);

            Rect rect2 = new Rect(
                Canvas.GetLeft(element2),
                Canvas.GetTop(element2),
                element2.ActualWidth,
                element2.ActualHeight);

            return rect1.IntersectsWith(rect2);
        }

        private void UpdateUI()
        {
            scoreText.Text = $"Счёт: {score}";
            levelText.Text = $"Уровень: {level}";
        }

        private void GameOver()
        {
            gameOver = true;
            gameTimer.Stop();
            gameOverText.Visibility = Visibility.Visible;
            restartButton.Visibility = Visibility.Visible;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left: isLeftPressed = true; break;
                case Key.Right: isRightPressed = true; break;
                case Key.Space: isSpacePressed = true; break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left: isLeftPressed = false; break;
                case Key.Right: isRightPressed = false; break;
                case Key.Space: isSpacePressed = false; break;
            }
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            InitGame();
            Focus();
        }
    }
}
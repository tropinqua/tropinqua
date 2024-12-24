using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace space_battle_v1
{
    public partial class MainWindow : Window
    {
        // Переменные для игры
        DispatcherTimer gameTimer = new DispatcherTimer();
        bool moveRight, moveLeft;
        List<Rectangle> itemRemover = new List<Rectangle>();
        Random rand = new Random();
        int enemyCounter = 100;
        int enemySpeed = 10;
        int limit = 50;
        int playerSpeed = 25;
        int score = 0;
        int hp = 3;
        Rect playerHitBox;

        // Переменные для работы с базой данных
        private string connectionString = "Data Source=game_scores.db;Version=3;";

        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabase();
            ShowMainMenu();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string createTableQuery = "CREATE TABLE IF NOT EXISTS Scores (Id INTEGER PRIMARY KEY AUTOINCREMENT, Score INTEGER)";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public void SaveScore(int score)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string insertQuery = "INSERT INTO Scores (Score) VALUES (@score)";
                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@score", score);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<int> GetTopScores(int topN = 10)
        {
            List<int> topScores = new List<int>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string selectQuery = $"SELECT Score FROM Scores ORDER BY Score DESC LIMIT {topN}";
                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            topScores.Add(reader.GetInt32(0));
                        }
                    }
                }
            }
            return topScores;
        }

        private void ShowMainMenu()
        {
            myCanvas.Visibility = Visibility.Hidden;
            MainMenuPanel.Visibility = Visibility.Visible;
            GameOverLabel.Visibility = Visibility.Hidden;
            ScorePoint.Content = "Score: 0";
            HealthPoint.Content = "HP: 10";
            score = 0;
            hp = 3;
            enemySpeed = 10;
            playerSpeed = 25;
            limit = 50;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Hidden;
            myCanvas.Visibility = Visibility.Visible;

            gameTimer.Interval = TimeSpan.FromMilliseconds(20);
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
            myCanvas.Focus();

            // Установка фона и игрока...
            ImageBrush bg = new ImageBrush();
            bg.ImageSource = new BitmapImage(new Uri("C:\\Users\\PC\\Desktop\\123\\icons/purple.png"));
            bg.TileMode = TileMode.Tile;
            bg.Viewport = new Rect(0, 0, 0.15, 0.15);
            bg.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
            myCanvas.Background = bg;

            ImageBrush playerImage = new ImageBrush();
            playerImage.ImageSource = new BitmapImage(new Uri("C:\\Users\\PC\\Desktop\\123\\icons/player.png"));
            player.Fill = playerImage;
            // Начальные значения
            score = 0;
            hp = 3;
        }

        private void RecordsButton_Click(object sender, RoutedEventArgs e)
        {
            var topScores = GetTopScores();
            string message = "Топ-10 лучших результатов:\n";
            for (int i = 0; i < topScores.Count; i++)
            {
                message += $"{i + 1}. {topScores[i]}\n";
            }
            MessageBox.Show(message);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void GameLoop(object sender, EventArgs e)
        {
            playerHitBox = new Rect(Canvas.GetLeft(player), Canvas.GetTop(player), player.Width, player.Height);
            enemyCounter -= 1;
            ScorePoint.Content = "Score: " + score;
            HealthPoint.Content = "HP: " + hp;
            if (enemyCounter < 0)
            {
                MakeEnemies();
                enemyCounter = limit;
            }

            if (moveLeft == true && Canvas.GetLeft(player) > 0)
            {
                Canvas.SetLeft(player, Canvas.GetLeft(player) - playerSpeed);
            }
            if (moveRight == true && Canvas.GetLeft(player) + 90 < Application.Current.MainWindow.Width)
            {
                Canvas.SetLeft(player, Canvas.GetLeft(player) + playerSpeed);
            }

            foreach (var x in myCanvas.Children.OfType<Rectangle>())
            {
                if (x is Rectangle && (string)x.Tag == "bullet")
                {
                    Canvas.SetTop(x, Canvas.GetTop(x) - 20);
                    Rect bulletHitBox = new Rect(Canvas.GetLeft(x), Canvas.GetTop(x), x.Width, x.Height);
                    if (Canvas.GetTop(x) < 10)
                    {
                        itemRemover.Add(x);
                    }
                    foreach (var y in myCanvas.Children.OfType<Rectangle>())
                    {
                        if (y is Rectangle && (string)y.Tag == "enemy")
                        {
                            Rect enemyHit = new Rect(Canvas.GetLeft(y), Canvas.GetTop(y), y.Width, y.Height);
                            if (bulletHitBox.IntersectsWith(enemyHit))
                            {
                                itemRemover.Add(x);
                                itemRemover.Add(y);
                                score++;
                            }
                        }
                    }
                }
                if (x is Rectangle && (string)x.Tag == "enemy")
                {
                    Canvas.SetTop(x, Canvas.GetTop(x) + enemySpeed);
                    if (Canvas.GetTop(x) > 750)
                    {
                        itemRemover.Add(x);
                        hp -= 1;
                    }
                    Rect enemyHitBox = new Rect(Canvas.GetLeft(x), Canvas.GetTop(x), x.Width, x.Height);
                    if (playerHitBox.IntersectsWith(enemyHitBox))
                    {
                        itemRemover.Add(x);
                        hp -= 1;
                    }
                }
            }

            foreach (Rectangle i in itemRemover)
            {
                myCanvas.Children.Remove(i);
            }

            if (score > 10)
            {
                limit = 50;
                enemySpeed = 10;
            }
            if (hp < 1)
            {
                gameTimer.Stop();
                HealthPoint.Content = "HP: 0";
                GameOverLabel.Content = $"ВЫ ПРОИГРАЛИ! \nОчки: {score}";
                GameOverLabel.Visibility = Visibility.Visible;

                // Сохранение результата в базе данных
                SaveScore(score);

                // Показать главное меню
                ShowMainMenu();

                return; // Остановить дальнейшую обработку.

            }

        }

        private new void KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                moveLeft = true;
            }
            if (e.Key == Key.Right)
            {
                moveRight = true;
            }
            if (e.Key == Key.Space)
            {
                Rectangle NewBullet = new Rectangle
                {
                    Tag = "bullet",
                    Height = 20,
                    Width = 3,
                    Fill = Brushes.White,
                    Stroke = Brushes.Azure,
                };
                Canvas.SetLeft(NewBullet, Canvas.GetLeft(player) + player.Width / 2.3);
                Canvas.SetTop(NewBullet, Canvas.GetTop(player) + NewBullet.Height);
                myCanvas.Children.Add(NewBullet);
            }
        }

        private new void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                moveLeft = false;
            }
            if (e.Key == Key.Right)
            {
                moveRight = false;
            }
        }

        private void MakeEnemies()
        {
            ImageBrush enemySprite = new ImageBrush();
            int enemyScriptCounter = rand.Next(1, 5);
            switch (enemyScriptCounter)
            {
                case 1:
                    enemySprite.ImageSource = new BitmapImage(new Uri("C:\\Users\\PC\\Desktop\\123\\icons/1.png"));
                    break;
                case 2:
                    enemySprite.ImageSource = new BitmapImage(new Uri("C:\\Users\\PC\\Desktop\\123\\icons/2.png"));
                    break;
                case 3:
                    enemySprite.ImageSource = new BitmapImage(new Uri("C:\\Users\\PC\\Desktop\\123\\icons/3.png"));
                    break;
                case 4:
                    enemySprite.ImageSource = new BitmapImage(new Uri("C:\\Users\\PC\\Desktop\\123\\icons/4.png"));
                    break;
                case 5:
                    enemySprite.ImageSource = new BitmapImage(new Uri("C:\\Users\\PC\\Desktop\\123\\icons/5.png"));
                    break;
            }
            Rectangle newEnemy = new Rectangle
            {
                Tag = "enemy",
                Height = 60,
                Width = 50,
                Fill = enemySprite
            };
            Canvas.SetTop(newEnemy, -100);
            Canvas.SetLeft(newEnemy, rand.Next(30, 400));
            myCanvas.Children.Add(newEnemy);
        }
    }
}
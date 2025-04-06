using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MazeGame
{
    class Program
    {
        static char[,] maze;
        static int mazeWidth, mazeHeight;
        static int playerX, playerY;
        static int healthPercent = 100;
        static List<(int x, int y)> enemies = new List<(int, int)>();
        static Random random = new Random();
        static int exitX, exitY;
        static bool enemiesVisible = true;

        static void Main(string[] args)
        {
            if (!File.Exists("maze.txt"))
            {
                Console.WriteLine("Файл maze.txt не найден!");
                return;
            }
            LoadMaze("maze.txt");
            exitX = mazeWidth - 2;
            exitY = mazeHeight - 2;
            playerX = 1;
            playerY = 1;
            Console.CursorVisible = false;
            SpawnEnemies(3);
            while (true)
            {
                if (healthPercent <= 0)
                {
                    Console.Clear();
                    Console.WriteLine("Game Over! Вы погибли!");
                    Thread.Sleep(2000);
                    break;
                }
                if (playerX == exitX && playerY == exitY)
                {
                    Console.Clear();
                    Console.WriteLine("You Win! Поздравляем!");
                    Thread.Sleep(2000);
                    break;
                }
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Escape)
                        break;
                    if (key == ConsoleKey.R)
                        ShowRoute();
                    else if (key == ConsoleKey.K)
                        enemiesVisible = !enemiesVisible;
                    else
                        HandlePlayerMove(key);
                }
                if (enemiesVisible)
                {
                    UpdateEnemies();
                    CheckEnemyCollision();
                }
                RedrawScreen();
                Thread.Sleep(200);
            }
        }

        static void LoadMaze(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            mazeHeight = lines.Length;
            mazeWidth = lines[0].Length;
            maze = new char[mazeHeight, mazeWidth];
            for (int y = 0; y < mazeHeight; y++)
                for (int x = 0; x < mazeWidth; x++)
                    maze[y, x] = lines[y][x];
        }

        static void RedrawScreen(List<(int x, int y)> path = null)
        {
            Console.Clear();
            for (int y = 0; y < mazeHeight; y++)
            {
                Console.SetCursorPosition(0, y);
                for (int x = 0; x < mazeWidth; x++)
                    Console.Write(maze[y, x]);
            }
            if (path != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                foreach (var (px, py) in path)
                    if (!(px == playerX && py == playerY))
                    {
                        Console.SetCursorPosition(px, py);
                        Console.Write('*');
                    }
                Console.ResetColor();
            }
            if (enemiesVisible)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var (ex, ey) in enemies)
                {
                    Console.SetCursorPosition(ex, ey);
                    Console.Write('E');
                }
                Console.ResetColor();
            }
            Console.SetCursorPosition(playerX, playerY);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write('@');
            Console.ResetColor();
            Console.SetCursorPosition(0, mazeHeight);
            DrawHealthBar(20, healthPercent);
            Console.SetCursorPosition(0, mazeHeight + 1);
            Console.WriteLine("Управление: Стрелки/WASD, R - маршрут, K - скрыть/показать врагов, Esc - выйти.");
        }

        static void DrawHealthBar(int width, int percent)
        {
            int filled = (int)Math.Round((percent / 100.0) * width);
            string bar = "[" + new string('#', filled) + new string('_', width - filled) + "]";
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(bar);
            Console.ResetColor();
        }

        static void HandlePlayerMove(ConsoleKey key)
        {
            int newX = playerX, newY = playerY;
            switch (key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    newY--; break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    newY++; break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    newX--; break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    newX++; break;
                default:
                    return;
            }
            if (IsWalkable(newX, newY))
            {
                playerX = newX;
                playerY = newY;
            }
        }

        static bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= mazeWidth || y < 0 || y >= mazeHeight)
                return false;
            return maze[y, x] != '#';
        }

        static void SpawnEnemies(int count)
        {
            enemies.Clear();
            int attempts = 0;
            while (enemies.Count < count && attempts < 1000)
            {
                int x = random.Next(1, mazeWidth - 1);
                int y = random.Next(1, mazeHeight - 1);
                if (IsWalkable(x, y) && (x != playerX || y != playerY))
                {
                    bool occupied = false;
                    foreach (var e in enemies)
                        if (e.x == x && e.y == y)
                        {
                            occupied = true;
                            break;
                        }
                    if (!occupied)
                        enemies.Add((x, y));
                }
                attempts++;
            }
        }

        static void UpdateEnemies()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                var (ex, ey) = enemies[i];
                int direction = random.Next(5);
                int newX = ex, newY = ey;
                switch (direction)
                {
                    case 0: newY--; break;
                    case 1: newY++; break;
                    case 2: newX--; break;
                    case 3: newX++; break;
                    default: break;
                }
                if (IsWalkable(newX, newY) && (newX != playerX || newY != playerY))
                    enemies[i] = (newX, newY);
            }
        }

        static void CheckEnemyCollision()
        {
            foreach (var (ex, ey) in enemies)
                if (ex == playerX && ey == playerY)
                    AttackPlayer();
        }

        static void AttackPlayer()
        {
            healthPercent -= 20;
            if (healthPercent < 0)
                healthPercent = 0;
        }

        static void ShowRoute()
        {
            var path = FindPathBFS(playerX, playerY, exitX, exitY);
            RedrawScreen(path);
            Console.SetCursorPosition(0, mazeHeight + 2);
            if (path.Count == 0)
                Console.Write("Маршрут не найден! Нажмите любую клавишу...");
            else
                Console.Write("Показан маршрут BFS! Нажмите любую клавишу...");
            Console.ReadKey(true);
        }

        static List<(int x, int y)> FindPathBFS(int startX, int startY, int endX, int endY)
        {
            if (!IsWalkable(startX, startY) || !IsWalkable(endX, endY))
                return new List<(int, int)>();
            bool[,] visited = new bool[mazeHeight, mazeWidth];
            var parent = new Dictionary<(int, int), (int, int)>();
            Queue<(int, int)> queue = new Queue<(int, int)>();
            queue.Enqueue((startX, startY));
            visited[startY, startX] = true;
            parent[(startX, startY)] = (-1, -1);
            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };
            bool found = false;
            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                if (cx == endX && cy == endY)
                {
                    found = true;
                    break;
                }
                for (int i = 0; i < 4; i++)
                {
                    int nx = cx + dx[i];
                    int ny = cy + dy[i];
                    if (nx >= 0 && nx < mazeWidth && ny >= 0 && ny < mazeHeight)
                    {
                        if (!visited[ny, nx] && IsWalkable(nx, ny))
                        {
                            visited[ny, nx] = true;
                            parent[(nx, ny)] = (cx, cy);
                            queue.Enqueue((nx, ny));
                        }
                    }
                }
            }
            if (!found)
                return new List<(int, int)>();
            var path = new List<(int, int)>();
            (int x, int y) cur = (endX, endY);
            while (cur.x != -1 && cur.y != -1)
            {
                path.Add(cur);
                cur = parent[cur];
            }
            path.Reverse();
            return path;
        }
    }
}

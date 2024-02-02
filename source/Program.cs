using System;
using System.IO;
using System.Timers;

namespace AutoBackup
{
    internal class Program
    {
        private string configPath = "save paths.txt";
        private static string sourcePath;
        private static string destinationPath;

        private static Timer timer;
        private double autosaveCooldown;
        private bool isAutosave = false;

        private void PrintLine(char c)
        {
            string line = string.Empty;
            for (byte i = 0; i < 40; i++)
                line += c;
            Console.ForegroundColor= ConsoleColor.DarkGray;
            Console.WriteLine(line);
            Console.ForegroundColor= ConsoleColor.White;
        }

        private void Clear()
        {
            Console.Clear();
            Console.ForegroundColor= ConsoleColor.DarkGray;
            Console.WriteLine("| \tFILES BACKUP MANAGER\t |");
            PrintLine('=');
            Console.ForegroundColor= ConsoleColor.White;
            Console.WriteLine("Введіть \'help\' для перегляду всіх команд");
        }

        private void SetSourcePath(string path)
        {
            if (CheckPath(path))
            {
                sourcePath = path;
                EditConfigLine(path, 0);
            }
            else
                Console.WriteLine("Введіть правильний шлях до гри");
        }

        private void SetDestinationPath(string path)
        {
            if (CheckPath(path, true))
            {
                destinationPath = path;
                EditConfigLine(path, 1);
            }
            else
                Console.WriteLine("Введіть правильний шлях до\nваших резервних копій");
        }

        private bool CheckPath(string path)
        {
            if (path == String.Empty)
            {
                Console.WriteLine("Шлях попрожній");
                return false;
            }

            if (!Directory.Exists(path))
            {
                Console.WriteLine($"Шлях \'{path}\' не існує");
                return false;
            }
            return true;
        }

        private bool CheckPath(string path, bool canCreate)
        {
            if (path == String.Empty)
            {
                Console.WriteLine("Шлях попрожній");
                return false;
            }

            if (Directory.Exists(path))
                return true;
            else
            {
                Console.WriteLine($"Шлях \'{path}\' не існує");

                if(!canCreate)
                    return false;

                Console.WriteLine($"Створити відповідний шлях:\n{path}?");
                if (OfferDesicion() == true)
                {
                    Directory.CreateDirectory(path);
                    return true;
                }
                return false;
            }
        }

        private void Save()
        {
            if (CheckPath(sourcePath) && CheckPath(destinationPath))
            {
                Directory.Delete(destinationPath, true);
                CopyDirectory(sourcePath, destinationPath);
                Console.WriteLine("Резервну копію успішно збережено");
            }
        }

        private static void Autosave(object sender, ElapsedEventArgs e)
        {
            Program program = new Program();

            if(program.CheckPath(sourcePath) && program.CheckPath(destinationPath))
            {
                Directory.Delete(destinationPath, true);
                CopyDirectory(sourcePath, destinationPath);
                Console.WriteLine($"Автозбереження ({System.DateTime.Now})");
            }
        }

        private void Load() 
        {
            if (CheckPath(sourcePath) && CheckPath(destinationPath))
            {
                Console.WriteLine("Ви впевнені, що хочете завантажити\nрезервну копію збереження?" +
                    "\n(поточне збереження буде видалено)");
                if (OfferDesicion() == false)
                    return;

                Directory.Delete(sourcePath, true);
                CopyDirectory(destinationPath, sourcePath);
                Console.WriteLine("Резервну копію успішно завантажено");
            }
        }

        private void TurnAutosave()
        {
            isAutosave = !isAutosave;
            Console.WriteLine(isAutosave ? "Автозбереження ввімкнено" : "Автозбереження вимкнено");
            EditConfigLine(isAutosave ? "enabled" : "disabled", 2);

            if (autosaveCooldown == 0)
            {
                Console.WriteLine("Значення таймера дорівнює нулю");
                return;
            }

            if (isAutosave)
            {
                if(timer == null)
                {
                    CreateTimer(autosaveCooldown);
                    return;
                }
                timer.Start();
            }

            if(isAutosave == false)
                timer.Stop();
        }

        private void SetAutosaveTimer(float cooldown, bool showMsg)
        {
            if (cooldown >= 1 && cooldown <= 100)
            {
                autosaveCooldown = cooldown;

                Program.timer = CreateTimer(cooldown);
                Program.timer.Stop();
                isAutosave = false;

                if(showMsg)
                    Console.WriteLine($"Таймер автозбереження\nвстановлено на {cooldown} хвилин");
                EditConfigLine(cooldown.ToString(), 3);
            }
            else
                Console.WriteLine("Неправильне значення таймера");
        }

        private string Input()
        {
            Console.ForegroundColor= ConsoleColor.Yellow;
            string command = Console.ReadLine().ToLower().Trim();
            Console.ForegroundColor= ConsoleColor.White;
            return command;
        }

        static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
                Console.WriteLine("Директорію {0} не знайдено", dir.FullName);

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            if (dirs.Length > 0)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir);
                }
            }
        }

        private void CreateConfig()
        {
            if (!File.Exists(configPath))
                File.Create(configPath);
        }

        private void GetConfigValues()
        {
            string[] values = File.ReadAllLines(configPath);
            if(values == null || values.Length < 2)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("(Деякі конфігураційні шляхи наразі пусті)");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }
            else
            {
                SetSourcePath(values[0]);
                SetDestinationPath(values[1]);

                if (values.Length == 2)
                    return;

                isAutosave = values[2] == "enabled" ? true : false;

                if (values.Length == 3)
                    return;

                float timer;
                if (float.TryParse(values[3], out timer))
                    SetAutosaveTimer(timer, isAutosave);
            }
        }

        private void EditConfigLine(string newLine, int lineIndex)
        {
            string[] lines = File.ReadAllLines(configPath);

            if(lineIndex > lines.Length - 1)
            {
                string[] newLines = new string[lineIndex + 1];
                for(byte i = 0; i < lines.Length; i++)
                    newLines[i] = lines[i];

                for(byte i = (byte)lines.Length; i < lineIndex; i++)
                    newLines[i] = String.Empty;

                newLines[lineIndex] = newLine;
                File.WriteAllLines(configPath, newLines);
            }
            else
            {
                lines[lineIndex] = newLine;
                File.WriteAllLines(configPath, lines);
            }

        }

        private Timer CreateTimer(double time)
        {
            timer = new Timer(TimeSpan.FromMinutes(time).TotalMilliseconds);
            timer.AutoReset = true;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(Autosave);
            timer.Start();
            return timer;
        }

        private bool OfferDesicion()
        {
            Console.WriteLine("[ yes / no ]");
            return Input() == "yes" ? true : false;
        }

        static void Main(string[] args)
        {
            Console.Title = "Backup Manager";
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Program program = new Program();

            program.CreateConfig();
            program.Clear();
            program.PrintLine('-');
            program.GetConfigValues();

            if (program.isAutosave)
                program.CreateTimer(program.autosaveCooldown);

            while (true)
            {
                string command = program.Input();
                switch (command)
                {
                    case "help":
                        Console.WriteLine("exit - вихід з програми\n" +
                            "clear - очистити консоль\n" +
                            "dest - задання шляху для збереження\n" +
                            "source - задання шляху з грою\n" +
                            "save - резервне копіювання\n" +
                            "load - завантаження резервної копії\n" +
                            "autosave - автозбереження\n" +
                            "timer - таймер автозбереження\n" +
                            "check - перевірити наявність файла\n" +
                            "paths - показати вказані шляхи\n" + 
                            "config - вивести файл конфігурації");
                        break;
                    case "exit":
                        return;
                    case "clear":
                        program.Clear();
                        break;
                    case "dest":
                        Console.WriteLine("Ведіть шлях, по якому будуть\nзберігатися резервні копії\n" +
                            "приклад: C:\\Stalker\\saves");
                        program.SetDestinationPath(program.Input() + "\\savedgames");
                        break;
                    case "source":
                        Console.WriteLine("Ведіть шлях до папки \nприклад: E:\\Files");
                        program.SetSourcePath(program.Input() + "\\");
                        break;
                    case "save":
                        program.Save();
                        break;
                    case "load":
                        program.Load();
                        break;
                    case "autosave":
                        program.TurnAutosave();
                        break;
                    case "timer":
                        Console.WriteLine("Введіть значення (в хвилинах)\nдля таймера автозбереження");
                        float timer;
                        if (float.TryParse(program.Input(), out timer))
                            program.SetAutosaveTimer(timer, true);
                        else
                            Console.WriteLine("Не вдається перетворити введене\nзначення на дійсне число");
                        break;
                    case "check":
                        if (program.CheckPath(program.Input()))
                            Console.WriteLine("Шлях існує");
                        break;
                    case "paths":
                        Console.WriteLine($"Шлях до гри: {sourcePath}\n" +
                            $"Шлях до резервних копій: {destinationPath}");
                        break;
                    case "config":
                        string[] lines = File.ReadAllLines(program.configPath);

                        if(lines.Length == 0)
                        {
                            Console.WriteLine("-пусто-");
                            break;
                        }

                        foreach (string line in lines)
                            Console.WriteLine(line);
                        break;
                    default: 
                        break;
                }
                program.PrintLine('-');
            }
        }
    }
}
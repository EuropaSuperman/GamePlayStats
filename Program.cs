using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Xml;
namespace GameDec
{
    public class GamePlayData
    {
        public string GameName;
        public DateTime StartTime = DateTime.MinValue;
        public DateTime EndTime = DateTime.MinValue;
        public string GameTrans;
        public TimeSpan GetPlayTimeSpan()
        {
            return EndTime - StartTime;
        }
        public string GetPlayTime()
        {
            return Utilitiy.TimeSpanString(EndTime - StartTime);
        }
        
        public GamePlayData(string gameName, string gameTrans)
        {
            GameName = gameName;
            GameTrans = gameTrans;
        }
        public GamePlayData()
        {
            GameName = "Undefined";
            GameTrans = "未知";
        }

    }


    public class Program
    {
        // 存储已注册进程的字典，避免重复注册 Exited 事件
        public static readonly Dictionary<int, GamePlayData> ProcessGameData = new Dictionary<int, GamePlayData>();
        public static List<GamePlayData> Games = new List<GamePlayData>();
        static void Main(string[] args)
        {
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                if (toastArgs.Argument == "action=viewHistory")
                {
                    string historyFilePath = MarkDownGen.HistoryFilePath;
                    string reportFilePath = MarkDownGen.ReportFilePath;
                    if (System.IO.File.Exists(historyFilePath) && System.IO.File.Exists(reportFilePath))
                    {

                        Process.Start(historyFilePath);
                        Console.WriteLine($"打开{historyFilePath}");
                        Process.Start(reportFilePath);
                        Console.WriteLine($"打开{reportFilePath}");

                    }
                    else
                    {
                        Console.WriteLine("历史文件md或者记录文件md不存在");
                    }
                }
            };
            string configFilePath = "config.xml";
            ReadConfig(configFilePath);
            while (true)
            {
                Thread.Sleep(3000);  // 每3秒检测一次
                foreach (var game in Games)
                {
                    MonitorGameProcess(game);
                }
            }
        }

        // 监控特定游戏进程
        static void MonitorGameProcess(GamePlayData game)
        {
            string gameName = game.GameName;
            var processes = Process.GetProcessesByName(gameName);

            foreach (var process in processes)
            {
                // 如果进程未被检测过，则设置事件监听
                if (!ProcessGameData.ContainsKey(process.Id))
                {
                    ProcessGameData.Add(process.Id,game);
                    process.EnableRaisingEvents = true;
                    process.Exited += (sender, e) => OnProcessExited(sender,e);
                    game.StartTime = DateTime.Now;
                    Console.WriteLine($"{gameName} 正在运行.");

                }
            }
        }

        // 进程退出时触发的事件
        static void OnProcessExited(object sender, EventArgs e)
        {
            Process process = sender as Process;
            if (process != null)
            {
                Console.WriteLine($"Process {process.ProcessName} has exited.");
                ProcessGameData[process.Id].EndTime = DateTime.Now;
                MarkDownGen.GenerateMDFile(ProcessGameData[process.Id]);
                // 使用 Toast 通知

                new ToastContentBuilder()
                    .AddText(ProcessGameData[process.Id].GameTrans)
                    .AddText("游戏已退出。您这次游玩了" + ProcessGameData[process.Id].GetPlayTime())
                    .AddButton(new ToastButton()
                        .SetContent("查看记录")
                        .AddArgument("action", "viewHistory")
                        .SetBackgroundActivation())
                    .Show();

            }
        }

        static void ReadConfig(string configFilePath)
        {
            // 使用 XmlReader 读取 XML
            using (XmlReader reader = XmlReader.Create(configFilePath))
            {
                // 遍历 XML 文件
                while (reader.Read())
                {
                    
                    if (reader.IsStartElement() && reader.Name == "ReportFilePath")
                    {
                        MarkDownGen.ReportFilePath = Environment.CurrentDirectory + "/" +  reader.ReadElementContentAsString();
                        continue;
                        
                    }

                    if (reader.IsStartElement() && reader.Name == "HistoryFilePath")
                    {
                        MarkDownGen.HistoryFilePath = Environment.CurrentDirectory + "/" + reader.ReadElementContentAsString();
                        continue;
                    }

                    if (reader.IsStartElement() && reader.Name == "HistoryXmlPath")
                    {
                        MarkDownGen.HistoryXmlPath = reader.ReadElementContentAsString();
                        //Console.WriteLine(MarkDownGen.HistoryXmlPath);
                        continue;
                    }

                    if (reader.IsStartElement() && reader.Name == "PicFilePath")
                    {
                        MarkDownGen.PicFilePath = reader.ReadElementContentAsString();
                        continue;
                    }
                    // 如果当前节点是 Game
                    if (reader.IsStartElement() && reader.Name == "Game")
                    {
                        string gameName = null;
                        string gameTrans = null;

                        // 遍历 Game 元素的子元素
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                if (reader.Name == "GameName")
                                {
                                    gameName = reader.ReadElementContentAsString();
                                }
                                else if (reader.Name == "GameTrans")
                                {
                                    gameTrans = reader.ReadElementContentAsString();
                                }
                            }

                            // 当 Game 元素的所有子元素读取完成时，跳出循环
                            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Game")
                            {
                                break;
                            }
                        }
                        //加载到缓存
                        if (gameName != null && gameTrans != null)
                        {
                            Games.Add(new GamePlayData(gameName, gameTrans));
                        }
                    }
                }
            }

        }
    }
}

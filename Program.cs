using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public string GetPlayTime()
        {
            TimeSpan PlayTime = EndTime - StartTime;
            StringBuilder Time = new StringBuilder();
            if (PlayTime.Days > 0)
            {
                Time.Append(PlayTime.Days + "天");
            }
            if (PlayTime.Hours > 0)
            {
                Time.Append(PlayTime.Hours + "小时");
            }
            if (PlayTime.Minutes > 0)
            {
                Time.Append(PlayTime.Minutes + "分钟");
            }
            if (PlayTime.Seconds > 0)
            {
                Time.Append(PlayTime.Seconds + "秒");
            }
            return Time.ToString();
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
            var process = sender as Process;
            if (process != null)
            {
                Console.WriteLine($"Process {process.ProcessName} has exited.");
                ProcessGameData[process.Id].EndTime = DateTime.Now;
                MarkDownGen.GenerateHistory(ProcessGameData[process.Id]);
                // 使用 Toast 通知
                new ToastContentBuilder()
                    .AddText(ProcessGameData[process.Id].GameTrans)
                    .AddText("游戏已退出。您这次游玩了" + ProcessGameData[process.Id].GetPlayTime())
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

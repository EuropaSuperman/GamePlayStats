using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
namespace GameDec
{
    public static class MarkDownGen
    {
        //在读取配置文件时初始化
        public static string HistoryFilePath;
        public static string ReportFilePath;
        public static string HistoryXmlPath;
        public static string PicFilePath;

        public static List<GamePlayData> HistoryGamePlayData = new List<GamePlayData>();

        //TODO：
        //反序列化历史数据文件，存储到HistoryGamePlayData中，然后计算相关历史数据。
        
        private static void DeserializeHistoryXmlFile()
        {
            if (!File.Exists(HistoryXmlPath))
            {
                using (var fileStream = File.Create(HistoryXmlPath))
                {
                    // 添加根元素
                    byte[] defaultContent = new UTF8Encoding(true).GetBytes("<GamePlayDataList></GamePlayDataList>");
                    fileStream.Write(defaultContent, 0, defaultContent.Length);
                }
                return;
            }
            else
            {
                var serializer = new XmlSerializer(typeof(List<GamePlayData>));
                using (var stream = new FileStream(HistoryXmlPath, FileMode.Open))
                {
                    if (stream.Length == 0)
                    {
                        // Handle empty file scenario
                        HistoryGamePlayData = new List<GamePlayData>();
                    }
                    else
                    {
                        HistoryGamePlayData = (List<GamePlayData>)serializer.Deserialize(stream);
                    }
                }
            }
        }

        private static void SerializeHistoryXmlFile()
        {
            var serializer = new XmlSerializer(typeof(List<GamePlayData>));
            using (var stream = new FileStream(HistoryXmlPath, FileMode.Open))
            {
                serializer.Serialize(stream, HistoryGamePlayData);
            }
        }

        //在游戏进程exit后调用,生成历史数据md文档，并序列化到Xml文件。
        public static void GenerateMDFile(GamePlayData gamePlayData)
        {
            //生成history.md
            DeserializeHistoryXmlFile();
            HistoryGamePlayData.Add(gamePlayData);
            SerializeHistoryXmlFile();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# 游戏历史数据\n");
            sb.AppendLine("| 游戏名 | 游戏时长 | 开始时间 | 结束时间 |");
            sb.AppendLine("| ------ | ------ | ------ | ------ |");

            TimeSpan totalPlayTime = new TimeSpan(0, 0, 0, 0);
            TimeSpan meanPlayTime = new TimeSpan(0, 0, 0, 0);
            foreach (var game in HistoryGamePlayData)
            {
                sb.AppendLine($"| {game.GameTrans} | {game.GetPlayTime()} | {game.StartTime} | {game.EndTime} |");
                totalPlayTime += game.GetPlayTimeSpan();
            }
            TimeSpan AverageDailyPlayTime = Utilitiy.GetAverageDailyPlayTime(HistoryGamePlayData);
            sb.AppendLine($"\n您的总游戏时长：**{Utilitiy.TimeSpanString(totalPlayTime)}**\n");
            sb.AppendLine($"您的每日平均游戏时长：**{Utilitiy.TimeSpanString(AverageDailyPlayTime)}**\n");
            foreach(var game in Program.Games)
            {
                TimeSpan AverageDailyPlayTimeByName = Utilitiy.GetAverageDailyPlayTimeByName(HistoryGamePlayData, game.GameName);
                if(AverageDailyPlayTimeByName != TimeSpan.Zero)
                {
                    sb.AppendLine($"您游玩**{game.GameTrans}**的历史每日平均游戏时长：**{Utilitiy.TimeSpanString(AverageDailyPlayTimeByName)}**\n");
                }
            }
            if (!File.Exists(HistoryFilePath))
            {
                File.Create(HistoryFilePath);
                Thread.Sleep(1000);
            }
            File.WriteAllText(HistoryFilePath, sb.ToString());

            GenerateReport(HistoryGamePlayData);
        }

        //生成report.md
        private static void GenerateReport(List<GamePlayData> history)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# 今日游戏数据统计\n");
            DateTime today = DateTime.Today;
            sb.AppendLine($"**今天是{today.Month}月{today.Day}日  {today.DayOfWeek}**\n");
            List<GamePlayData> todayGamePlayData = new List<GamePlayData>();
            TimeSpan todayGamePlayTime = TimeSpan.Zero;
            foreach (var game in history)
            {
                if (game.GameName == "Undefined")
                {
                    Console.WriteLine("出现了不合法的GamePlayData");
                    continue;
                }
                if (game.StartTime.Date == DateTime.Today)
                {
                    todayGamePlayData.Add(game);
                    todayGamePlayTime += game.GetPlayTimeSpan();
                }
            }
            sb.AppendLine($"今日游戏总时长：**{Utilitiy.TimeSpanString(todayGamePlayTime)}**\n");
            sb.AppendLine("## 游玩记录\n");
            Dictionary<string, TimeSpan> gamePlayTime = new Dictionary<string, TimeSpan>();
            foreach (var game in todayGamePlayData)
            {
                if(!gamePlayTime.ContainsKey(game.GameTrans))
                {
                    gamePlayTime.Add(game.GameTrans, game.GetPlayTimeSpan());
                }
                else
                {
                    gamePlayTime[game.GameTrans] += game.GetPlayTimeSpan();
                }
                sb.AppendLine($"- 游戏名：{game.GameTrans}\n");
                sb.AppendLine($"  - 游戏时长：{game.GetPlayTime()}\n");
                sb.AppendLine($"  - 开始时间：{game.StartTime}\n");
                sb.AppendLine($"  - 结束时间：{game.EndTime}\n");
                sb.AppendLine($"  - 时间占比：{(game.GetPlayTimeSpan().Ticks / (double)todayGamePlayTime.Ticks * 100):F2}%\n");
            }
            sb.AppendLine("## 游戏时长统计\n");
            foreach (KeyValuePair<string, TimeSpan> p in gamePlayTime)
            {
                sb.AppendLine($"**{p.Key}**的总游戏时间为**{Utilitiy.TimeSpanString(p.Value)}**，占今日游玩时间的{(p.Value.Ticks / (double)todayGamePlayTime.Ticks * 100):F2}%\n");
            }


            if (!File.Exists(ReportFilePath))
            {
                File.Create(ReportFilePath);
                Thread.Sleep(10);
            }
            File.WriteAllText(ReportFilePath, sb.ToString());
        }
    }
}

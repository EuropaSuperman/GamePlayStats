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
        public static void GenerateHistory(GamePlayData gamePlayData)
        {
            DeserializeHistoryXmlFile();
            HistoryGamePlayData.Add(gamePlayData);
            SerializeHistoryXmlFile();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# 游戏数据统计\n");
            sb.AppendLine("## 游戏历史数据\n");
            sb.AppendLine("| 游戏名 | 游戏时长 | 开始时间 | 结束时间 |");
            sb.AppendLine("| ------ | ------ | ------ | ------ |");
            foreach (var game in HistoryGamePlayData)
            {
                sb.AppendLine($"| {game.GameName} | {game.GetPlayTime()} | {game.StartTime} | {game.EndTime} |");
            }
            
           
            if (!File.Exists(HistoryFilePath))
            {
                File.Create(HistoryFilePath);
                Thread.Sleep(1000);
            }
            File.WriteAllText(HistoryFilePath, sb.ToString());
        }
    }
}

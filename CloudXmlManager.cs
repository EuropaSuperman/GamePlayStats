using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Web.Http;
namespace GameDec
{
    public static class CloudXmlManager
    {
        public static string ServerUrl;

        /// <summary>
        /// 上传本地历史文件到云端（覆盖）
        /// </summary>
        public static async Task UploadToCloudAsync()
        {
            if (ServerUrl == null || ServerUrl == "")
            {
                return;
            }
            var client = new System.Net.Http.HttpClient();

            try
            {
                // 读取本地文件
                var xmlContent = File.ReadAllText(MarkDownGen.HistoryXmlPath);

                // 发送PUT请求
                var response = await client.PutAsync(
                    $"{ServerUrl}/api/history",
                    new StringContent(xmlContent, System.Text.Encoding.UTF8, "application/xml")
                );

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"上传失败: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("上传失败", ex);
            }
        }

        /// <summary>
        /// 从云端合并历史记录到本地
        /// </summary>
        public static async Task SyncFromCloudAsync()
        {
            if (ServerUrl == null || ServerUrl == "")
            {
                return;
            }
            var client = new System.Net.Http.HttpClient();

            try
            {
                // 获取本地XML
                var localDoc = XDocument.Load(MarkDownGen.HistoryXmlPath);
                var localRoot = localDoc.Root;

                // 获取云端XML
                var response = await client.GetAsync($"{ServerUrl}/api/history");
                var cloudXml = await response.Content.ReadAsStringAsync();
                var cloudDoc = XDocument.Parse(cloudXml);
                var cloudRoot = cloudDoc.Root;

                // 创建合并字典（游戏名+开始时间作为唯一键）
                var existingRecords = localRoot.Elements("GamePlayData")
                    .ToDictionary(e => $"{e.Element("GameName").Value}|{e.Element("StartTime").Value}");

                if(existingRecords.Count == 0)
                {
                    // 本地文件为空，直接覆盖
                    localDoc = cloudDoc;
                    localDoc.Save(MarkDownGen.HistoryXmlPath);
                    return;
                }
                // 合并新记录
                foreach (var cloudElem in cloudRoot.Elements("GamePlayData"))
                {
                    var key = $"{cloudElem.Element("GameName").Value}|{cloudElem.Element("StartTime").Value}";

                    if (!existingRecords.ContainsKey(key))
                    {
                        localRoot.Add(cloudElem);
                        existingRecords.Add(key, null);
                    }
                }

                // 保存合并结果
                localDoc.Save(MarkDownGen.HistoryXmlPath);
            }
            catch (Exception ex)
            {
                throw new Exception("Sync operation failed", ex);
            }
        }
    }
}

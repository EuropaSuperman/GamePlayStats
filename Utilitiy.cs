using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameDec
{
    public static class Utilitiy
    {
        public static string TimeSpanString(TimeSpan t)
        {
            StringBuilder Time = new StringBuilder();
            if (t.Days > 0)
            {
                Time.Append(t.Days + "天");
            }
            if (t.Hours > 0)
            {
                Time.Append(t.Hours + "小时");
            }
            if (t.Minutes > 0)
            {
                Time.Append(t.Minutes + "分钟");
            }
            Time.Append(t.Seconds + "秒");
            return Time.ToString();
        }
        /// <summary>
        /// 获取平均总的每日游戏时长
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public static TimeSpan GetAverageDailyPlayTime(List<GamePlayData> datas)
        {
            TimeSpan ats = TimeSpan.Zero;
            Dictionary<DateTime, TimeSpan> dailyPlayTime = new Dictionary<DateTime, TimeSpan>();
            if (datas == null)
            {
                return ats;
            }
            if (datas.Count == 0)
            {
                return ats;
            }
            foreach (var data in datas)
            {
                if (!dailyPlayTime.ContainsKey(data.StartTime.Date)) 
                {
                    dailyPlayTime.Add(data.StartTime.Date,data.GetPlayTimeSpan());
                }
                else
                {
                    dailyPlayTime[data.StartTime.Date] += data.GetPlayTimeSpan();
                }
            }
            foreach (var timeSpan in dailyPlayTime.Values)
            {
                ats += timeSpan;
            }
            ats = TimeSpan.FromTicks(ats.Ticks / dailyPlayTime.Count);

            return ats;
        }

        public static TimeSpan GetAverageDailyPlayTimeByName(List<GamePlayData> datas,string name)
        {
            List<GamePlayData> m_datas = datas.Where(data => data.GameName == name).ToList();
            return GetAverageDailyPlayTime(m_datas);
        }
        /// <summary>
        /// 平均登录时间
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public static TimeSpan GetAverageLoginTime(List<GamePlayData> datas)
        {
            TimeSpan ast = TimeSpan.Zero;
            if (datas == null)
            {
                return ast;
            }
            if (datas.Count == 0)
            {
                return ast;
            }
            long totalTicks = 0;
            foreach (var data in datas)
            {
                DateTime basetime = data.StartTime.Date;
                totalTicks += (data.StartTime - basetime).Ticks;
            }
            long averageTicks = totalTicks / datas.Count;
            ast = TimeSpan.FromTicks(averageTicks);
            return ast;
        }

        public static TimeSpan GetAverageLoginTimeByName(List<GamePlayData> datas, string name)
        {
            List<GamePlayData> m_datas = datas.Where(data => data.GameName == name).ToList();
            return GetAverageLoginTime(m_datas);
        }

        
    }
}

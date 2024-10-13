using Newtonsoft.Json;
using TShockAPI;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace OneBlockChallenge
{
    internal class Configuration
    {
        #region 启用插件
        [JsonProperty("启用插件", Order = -10)]
        public bool Enabled { get; set; } = true;
        #endregion

        #region BOSS对应图格
        [JsonProperty("BOSS对应图格", Order = 0)]
        public Dictionary<string, List<int>> BossTile { get; set; } = new Dictionary<string, List<int>>()
        {
             {"无限制", new List<int> { }},
             {"史莱姆王", new List<int> { }},
             {"克苏鲁之眼", new List<int> { }},
             {"世界吞噬者", new List<int> { }},
             {"克苏鲁之脑", new List<int> { }},
             {"邪恶BOSS", new List<int> { }},
             {"骷髅王", new List<int> { }},
             {"蜂王", new List<int> { }},
             {"鹿角怪", new List<int> { }},
             {"血肉墙", new List<int> { }},
             {"一王", new List<int> { }},
             {"魔焰眼", new List<int> { }},
             {"激光眼", new List<int> { }},
             {"双子魔眼", new List<int> { }},
             {"毁灭者", new List<int> { }},
             {"机械骷髅王", new List<int> { }},
             {"三王", new List<int> { }},
             {"世纪之花", new List<int> { }},
             {"石巨人", new List<int> { }},
             {"猪鲨", new List<int> { }},
             {"拜月教邪教徒", new List<int> { }},
             {"月球领主", new List<int> { }},
             {"光之女皇", new List<int> { }},
             {"史莱姆皇后", new List<int> { }},
        };
        #endregion

        #region BOSS击败检测
        [JsonProperty("BOSS击败检测", Order = 0)]
        public Dictionary<string, bool> BossDefeated { get; set; } = new Dictionary<string, bool>()
        {
             {"无限制", true},
             {"史莱姆王", false},
             {"克苏鲁之眼", false},
             {"世界吞噬者", false},
             {"克苏鲁之脑", false},
             {"邪恶BOSS", false},
             {"骷髅王", false},
             {"蜂王", false},
             {"鹿角怪", false},
             {"血肉墙", false},
             {"一王", false},
             {"魔焰眼", false},
             {"激光眼", false},
             {"双子魔眼", false},
             {"毁灭者", false},
             {"机械骷髅王", false},
             {"三王", false},
             {"世纪之花", false},
             {"石巨人", false},
             {"猪鲨", false},
             {"拜月教邪教徒", false},
             {"月球领主", false},
             {"光之女皇", false},
             {"史莱姆皇后", false},
        };
        #endregion

        #region Broadcast
        [JsonProperty("广播设置", Order = 5)]
        public Dictionary<string, bool> Broadcast { get; set; } = new Dictionary<string, bool>()
        {
             {"所有BOSS", true},
             {"邪恶BOSS", true},
             {"一王", true},
             {"三王", true},
             {"调试广播", false},
             {"生成地图广播",true},
        };
        #endregion

        [JsonProperty("生成地图时随机位置", Order = -9)]
        public bool MapRandomPlace { get; set; } = false;

        [JsonProperty("围液体的方块", Order = -9)]
        public int LiquidBlock { get; set; } = 0;

        #region 读取与创建配置文件方法
        public static readonly string FilePath = Path.Combine(TShock.SavePath, "单方块挑战.json");

        public void Write()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }

        public static Configuration Read()
        {
            if (!File.Exists(FilePath))
            {

                var newConfig = new Configuration();
                newConfig.InitializeBossPresets(); // 初始化 Boss 预设参数
                newConfig.Write(); // 写入初始配置文件
                TShock.Log.ConsoleError("[OneBlockChallenge] 未找配置文件，已新建预设");
                return newConfig;
            }
            else
            {
                string jsonContent = File.ReadAllText(FilePath);
                return JsonConvert.DeserializeObject<Configuration>(jsonContent)!;
            }
        }
        #endregion

        #region 预设参数方法
        private void InitializeBossPresets()
        {
            BossTile = new Dictionary<string, List<int>>()
            {
                {"无限制", new List<int> {1,6,7,9,30,39,40,46,47,51,53,54,57,59,63,64,65,66,67
                ,112,123,145,146,147,148,151,161,166,167,168,175,176,188,189,190,191,196,202,206,224
                ,229,251,252,273,274,396,397,404,407,472,473}},
                {"史莱姆王", new List<int> {8,45,169,177,193,197,198,68}},
                {"克苏鲁之眼", new List<int> {22,140 }},
                {"世界吞噬者", new List<int> { }},
                {"克苏鲁之脑", new List<int> { }},
                {"邪恶BOSS", new List<int> {25,37,56,58,75,76,152,157,163,195,200,203,204,234,370,398,399,400,401}},
                {"骷髅王", new List<int> {41,43,44,48,194 }},
                {"蜂王", new List<int> {158,230 }},
                {"鹿角怪", new List<int> { }},
                {"血肉墙", new List<int> {107,108,111,116,117,118,121,122,119,120,150,159,160,164,221,222,223,402,403}},
                {"一王", new List<int> { }},
                {"魔焰眼", new List<int> { }},
                {"激光眼", new List<int> { }},
                {"双子魔眼", new List<int> { }},
                {"毁灭者", new List<int> { }},
                {"机械骷髅王", new List<int> { }},
                {"三王", new List<int> { 211, 346}},
                {"世纪之花", new List<int> { 253 }},
                {"石巨人", new List<int> {226,232 }},
                {"猪鲨", new List<int> { }},
                {"拜月教邪教徒", new List<int> { }},
                {"月球领主", new List<int> { }},
                {"光之女皇", new List<int> { }},
                {"史莱姆皇后", new List<int> { }},
            };

            BossDefeated = new Dictionary<string, bool>()
            {
                {"无限制", true},
                {"史莱姆王", false},
                {"克苏鲁之眼", false},
                {"世界吞噬者", false},
                {"克苏鲁之脑", false},
                {"邪恶BOSS", false},
                {"骷髅王", false},
                {"蜂王", false},
                {"鹿角怪", false},
                {"血肉墙", false},
                {"一王", false},
                {"魔焰眼", false},
                {"激光眼", false},
                {"双子魔眼", false},
                {"毁灭者", false},
                {"机械骷髅王", false},
                {"三王", false},
                {"世纪之花", false},
                {"石巨人", false},
                {"猪鲨", false},
                {"拜月教邪教徒", false},
                {"月球领主", false},
                {"光之女皇", false},
                {"史莱姆皇后", false},
            };

            Broadcast = new Dictionary<string, bool>()
            {
                {"所有BOSS", true},
                {"邪恶BOSS", true},
                {"一王", true},
                {"三王", true},
                {"调试广播", false},
                {"生成地图广播",true},
            };
            #endregion
        }
    }
}
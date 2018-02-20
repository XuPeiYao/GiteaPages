using Newtonsoft.Json;
using System;
using System.IO;

namespace GiteaPages.Models {
    /// <summary>
    /// GiteaPages設定
    /// </summary>
    public class GiteaPagesConfiguration {
        public string ConfigPath { get; private set; }

        /// <summary>
        /// 根目錄
        /// </summary>
        public string Root { get; set; }

        /// <summary>
        /// 預設首頁
        /// </summary>
        public string[] Defaults { get; set; }

        /// <summary>
        /// HTTP Status 404畫面
        /// </summary>
        public string NotFound { get; set; }

        /// <summary>
        /// 腳本注入
        /// </summary>
        public string[] Scripts { get; set; }

        /// <summary>
        /// 讀取設定
        /// </summary>
        /// <param name="path">路徑</param>
        /// <returns>設定</returns>
        public static GiteaPagesConfiguration Load(string path) {
            var instance = JsonConvert.DeserializeObject<GiteaPagesConfiguration>(File.ReadAllText(path));
            instance.ConfigPath = path;

            var dirPath = Path.GetDirectoryName(path);

            if (instance.NotFound != null && !instance.NotFound.Contains(":")) {
                instance.NotFound = Path.Combine(dirPath, instance.Root, instance.NotFound);
            }

            if (instance.Scripts != null) {
                for (int i = 0; i < instance.Scripts.Length; i++) {
                    if (instance.Scripts[i].Contains(":")) { // 絕對路徑
                        continue;
                    }
                    instance.Scripts[i] = Path.Combine(dirPath, instance.Root, instance.Scripts[i]);
                }
            }

            // 不允許路徑中含有 ..
            if (instance.Root != null && instance.Root.Contains("..")) {
                instance.Root = null;
            }
            // 不允許路徑中含有 ..
            if (instance.NotFound != null && instance.NotFound.Contains("..")) {
                instance.NotFound = null;
            }

            return instance;
        }

        /// <summary>
        /// 複製
        /// </summary>
        public GiteaPagesConfiguration Clone() {
            var props = typeof(GiteaPagesConfiguration).GetProperties();

            var instance = new GiteaPagesConfiguration();

            foreach (var prop in props) {
                var value = prop.GetValue(this);
                prop.SetValue(instance, value);
            }

            return instance;
        }

        /// <summary>
        /// 合併設定
        /// </summary>
        /// <param name="config">GiteaPages設定實例，用以覆蓋目前設定</param>
        /// <returns>合併後的設定</returns>
        public GiteaPagesConfiguration Merge(GiteaPagesConfiguration config) {
            var instance = Clone();
            var props = typeof(GiteaPagesConfiguration).GetProperties();

            foreach (var prop in props) {
                var value = prop.GetValue(config);
                if (value == null) continue;

                prop.SetValue(instance, value);
            }

            return instance;
        }
    }
}

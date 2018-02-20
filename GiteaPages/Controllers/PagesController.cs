using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GiteaPages.Models;
using GiteaPages.Models.Lite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GiteaPages.Controllers {
    public partial class PagesController : Controller {
        /// <summary>
        /// Lock物件清除迴圈
        /// </summary>
        private static Task LockClear;

        /// <summary>
        /// 下載鎖定
        /// </summary>
        private static Dictionary<string, object> DownloadLocker = new Dictionary<string, object>();

        /// <summary>
        /// 鎖定數量
        /// </summary>
        private static Dictionary<string, int> Locking = new Dictionary<string, int>();

        /// <summary>
        /// 資料庫
        /// </summary>
        public GiteaPagesDatabase Database { get; private set; }

        /// <summary>
        /// 應用程式設定
        /// </summary>
        public IConfiguration AppConfiguration { get; private set; }


        public GiteaPagesConfiguration Configuration { get; set; }

        public PagesController(
            GiteaPagesDatabase database,
            IConfiguration appConfiguration,
            GiteaPagesConfiguration defaultConfiguration) {
            this.Database = database;
            this.AppConfiguration = appConfiguration;
            this.Configuration = defaultConfiguration;
            if (LockClear == null) {
                LockClear = Task.Run(() => {
                    for (; ; ) {
                        Thread.Sleep(TimeSpan.FromSeconds(30));
                        lock (Locking) {
                            var willDelete = Locking.Where(x => x.Value == 0).ToArray();

                            foreach (var commit in willDelete) {
                                DownloadLocker.Remove(commit.Key);
                                Locking.Remove(commit.Key);
                            }
                        }
                    }
                });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GiteaPages.Models;
using GiteaPages.Models.Lite;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace GiteaPages.Controllers {
    public partial class PagesController : Controller {
        /// <summary>
        /// 取得指定使用者之儲存體目標路徑檔案內容
        /// </summary>
        /// <param name="user">使用者帳號</param>
        /// <param name="repo">儲存體名稱</param>
        /// <param name="path">檔案路徑</param>
        /// <returns>檔案內容</returns>
        [Route("{user}/{repo}/{*path}")]
        public async Task<ActionResult> Get(
            [FromRoute] string user,
            [FromRoute] string repo,
            [FromRoute] string path) {
            // 路徑不該為 null
            if (path == null) path = string.Empty;

            // 取得瀏覽版本CommitId
            var commitId = await GetCommitId(user, repo);

            // 無法獲取CommitId則返回404
            if (commitId == null) {
                // 沒有CommitId，則無法取得自訂404畫面，返回系統預設404
                return await FileWithStatusCode(path, Configuration.NotFound, 404);
            }

            // 下載儲存庫
            bool downloadSuccess = await DownloadRepo(user, repo, commitId);
            if (!downloadSuccess) {
                return await FileWithStatusCode(path, Configuration.NotFound, 404);
            }

            // 取得快取目錄
            var rootPath = GetRepoCacheDirPath(user, repo, commitId);

            // 讀取儲存庫giteaPages設定檔
            string configPath = Path.Combine(rootPath, "giteaPages.conf.json");
            var DefaultConfiguration = Configuration; // 備份系統設定預設值
            if (System.IO.File.Exists(configPath)) {
                // 合併設定
                Configuration = Configuration.Merge(GiteaPagesConfiguration.Load(configPath));
            }

            rootPath = Path.Combine(rootPath, Configuration.Root ?? "");

            // 未設定路徑則使用預設首頁
            if (string.IsNullOrWhiteSpace(path)) {
                if (Configuration.Defaults == null) { // 查無設定
                    return await FileWithStatusCode(path, Configuration.NotFound, 404);
                }
                foreach (var def in Configuration.Defaults) {
                    path = def;
                    if (System.IO.File.Exists(Path.Combine(rootPath, path))) break;
                }
            }

            // 完整檔案路徑
            string fullPath = Path.Combine(rootPath, path);

            // 找不到指定檔案，使用儲存庫內的404.html
            if (!System.IO.File.Exists(fullPath)) {
                fullPath = Configuration.NotFound;
                // 儲存庫內有404
                if (System.IO.File.Exists(fullPath)) {
                    return await FileWithStatusCode(path, fullPath, 404);
                } else { // 儲存庫內沒有404
                    return await FileWithStatusCode(path, DefaultConfiguration.NotFound, 404);
                }
            }

            return await FileWithStatusCode(path, fullPath);
        }

        [NonAction]
        public async Task<bool> DownloadRepo(string user, string repo, string commitId) {
            var cacheDir = Path.Combine(AppConfiguration["cacheDir"], user, repo, commitId);
            //已經有快取項目
            if (Directory.Exists(cacheDir)) {
                return true;
            }

            // 檢查目前是否正在下載中了
            lock (DownloadLocker) {
                // 沒在下載中
                if (!DownloadLocker.ContainsKey(commitId)) {
                    // 建立lock物件
                    DownloadLocker[commitId] = new object();
                }
            }

            // 標註正在被locking的數量
            lock (Locking) {
                if (!Locking.ContainsKey(commitId)) {
                    Locking[commitId] = 0;
                }
                Locking[commitId]++;
            }

            lock (DownloadLocker[commitId]) {
                // 如果快取項目，其他request在lock期間已經完成下載
                if (!Directory.Exists(cacheDir)) {
                    var url = $"{AppConfiguration["giteaHost"]}/{user}/{repo}/archive/{commitId}.zip";
                    HttpClient client = new HttpClient();

                    Stream downloadStream = null;
                    try {
                        downloadStream = client.GetStreamAsync(url).GetAwaiter().GetResult();
                    } catch { // 無法取得串流
                        return false;
                    }

                    using (ZipArchive arch = new ZipArchive(downloadStream, ZipArchiveMode.Read, true)) {
                        arch.ExtractToDirectory(cacheDir, true);
                    }
                }
            }

            // 解鎖
            Locking[commitId]--;
            return true;
        }

        [NonAction]
        public string GetRepoCacheDirPath(string user, string repo, string commitId) {
            string path = Path.Combine(AppConfiguration["cacheDir"], user, repo, commitId);

            return Directory.GetDirectories(path).First();
        }

        [NonAction]
        public async Task<FileStreamResult> FileWithStatusCode(string path, string filePath, int statusCode = 200) {
            Response.StatusCode = statusCode;

            var fileStream =
                System.IO.File.Open(
                    filePath,
                    System.IO.FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

            #region ContentType設定
            var fileExtension = Path.GetExtension(filePath);

            var extProvider = new FileExtensionContentTypeProvider();

            var contentType = "application/octet-stream";
            if (extProvider.Mappings.ContainsKey(fileExtension)) {
                contentType = extProvider.Mappings[fileExtension];
            }
            #endregion

            // 腳本注入
            if (contentType == "text/html" &&
                Configuration.Scripts != null &&
                Configuration.Scripts.Length > 0) {
                var doc = new HtmlDocument();
                doc.Load(fileStream);
                var htmlBody = doc.DocumentNode.SelectSingleNode("//body");

                foreach (var src in Configuration.Scripts) {
                    var script = System.IO.File.ReadAllText(src);
                    var node = HtmlNode.CreateNode($"<script>{script}</script>");
                    htmlBody.AppendChild(node);
                }

                var stream = new MemoryStream(Encoding.UTF8.GetBytes(doc.DocumentNode.OuterHtml));
                return File(stream, contentType);
            }

            return File(fileStream, contentType);
        }

        [NonAction]
        public async Task<string> GetCommitId(string user, string repo) {
            // 取得cookie中指定的版本
            Request.Cookies.TryGetValue($"{user}-{repo}", out string commit);

            // 如果取得的版本為空字串或null則重設為null
            if (string.IsNullOrWhiteSpace(commit)) commit = null;

            // 假設使用者沒有指定commitId，則嘗試呼叫API取得最新的commitId
            if (commit == null) {
                commit = await GetLastCommitId(user, repo);

                // 寫入資料庫
                if (commit != null) {
                    Database.CreateOrUpdateRecord(new RepositoryRecord() {
                        User = user.ToLower(),
                        Name = repo.ToLower(),
                        LastMasterCommitId = commit
                    });
                }
            }

            // 假設前者API請求成功則commitId則不應該為null，如為null則表示該repo不存在或無法存取
            // 將commitId設為cache內最新的項目
            if (commit == null) {
                var lastCommintInfo = Database.Get(user, repo);

                if (lastCommintInfo != null) {
                    commit = lastCommintInfo.LastMasterCommitId;
                }
            }

            return commit;
        }

        [NonAction]
        public async Task<string> GetLastCommitId(string user, string repo) {
            string url = $"{AppConfiguration["giteaHost"]}/api/v1/repos/{user}/{repo}/branches/master";

            try {
                HttpClient client = new HttpClient();
                var responseObj = JObject.Parse(await client.GetStringAsync(url));

                return responseObj["commit"].Value<string>("id").Substring(0, 10);
            } catch {
                return null;
            }
        }
    }
}

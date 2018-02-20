using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GiteaPages.Models.Lite;
using Microsoft.AspNetCore.Mvc;

namespace GiteaPages.Controllers {
    public partial class PagesController : Controller {
        /// <summary>
        /// 切換瀏覽版本
        /// </summary>
        /// <param name="user">使用者帳號</param>
        /// <param name="repo">儲存體名稱</param>
        /// <param name="path">檔案路徑</param>
        /// <param name="ref">取得指定branche/commit/tag</param>
        [Route("{user}/{repo}-{ref}/{*path}")]
        [Route("{user}/{repo}-last/{*path}")]
        public async Task<ActionResult> ChangeVersion(
            [FromRoute] string user,
            [FromRoute] string repo,
            [FromRoute] string path,
            [FromRoute] string @ref) {
            if (@ref == "last" || @ref == "master") {
                @ref = "";
            } else { // 檢查是否為完整的SHA
                Regex shaFormat = new Regex(@"\A\b[0-9a-fA-F]{40}\b\Z");
                if (shaFormat.IsMatch(@ref)) {
                    @ref = @ref.Substring(0, 10); // 簡碼
                }
            }

            Response.Cookies.Append($"{user}-{repo}", @ref?.ToLower());

            return Redirect($"/{user}/{repo}/{path}");
        }
    }
}

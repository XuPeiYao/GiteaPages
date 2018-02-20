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
        /// 重定向至指定使用者的profile儲存庫
        /// </summary>
        /// <param name="user">使用者帳號</param>
        [Route("{user}")]
        public async Task<ActionResult> UserProfile(
            [FromRoute] string user) {
            return Redirect($"/{user}/profile/");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace GiteaPages.Models.Lite {
    /// <summary>
    /// GiteaPages儲存庫紀錄
    /// </summary>
    public class RepositoryRecord {
        /// <summary>
        /// 唯一識別號
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 使用者唯一識別號
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Master分支最後CommitId
        /// </summary>
        public string LastMasterCommitId { get; set; }
    }
}

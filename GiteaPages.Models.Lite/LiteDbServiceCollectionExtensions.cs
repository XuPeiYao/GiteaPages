using GiteaPages.Models.Lite;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection {
    public static class LiteDbServiceCollectionExtensions {
        /// <summary>
        /// 加入LiteDatabase服務
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="path">資料庫檔案物件</param>
        public static IServiceCollection AddLiteDb(this IServiceCollection services, string path) {
            return services.AddScoped<GiteaPagesDatabase>(s => new GiteaPagesDatabase(path));
        }
    }
}

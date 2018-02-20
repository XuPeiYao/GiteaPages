using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GiteaPages.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GiteaPages {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services) {
            services.AddLiteDb(@"giteaPages.db");

            services.AddScoped<GiteaPagesConfiguration>(x => GiteaPagesConfiguration.Load("giteaPages.conf.json"));

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseNoCache();

            app.UseMvc();
        }
    }
}

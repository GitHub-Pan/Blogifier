using Core;
using Core.Data;
using Core.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using System.Linq;
using System.Threading;

namespace App
{
    public class Program
    {
        private static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<AppDbContext>();
                try
                {
                    if (context.Database.GetPendingMigrations().Any())
                    {
                        context.Database.Migrate();
                    }
                }
                catch { }

                var userMgr = (UserManager<AppUser>)services.GetRequiredService(typeof(UserManager<AppUser>));
                if (!userMgr.Users.Any())
                {
                    userMgr.CreateAsync(new AppUser { UserName = "admin", Email = "admin@us.com" }, "admin").Wait();
                    userMgr.CreateAsync(new AppUser { UserName = "demo", Email = "demo@us.com" }, "demo").Wait();
                }

                // load application settings from appsettings.json
                var app = services.GetRequiredService<IAppService<AppItem>>();
                AppConfig.SetSettings(app.Value);

                if (!context.BlogPosts.Any())
                {
                    try
                    {
                        services.GetRequiredService<IStorageService>().Reset();
                    }
                    catch { }

                    AppData.Seed(context);
                }
            }

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            dynamic type = (new Program()).GetType();
            string contentRoot = Path.GetDirectoryName(type.Assembly.Location);
#if DEBUG
            contentRoot = Directory.GetCurrentDirectory();
#endif
            var webRoot = Path.Combine(contentRoot, "wwwroot");
            return WebHost.CreateDefaultBuilder(args)
              .UseContentRoot(contentRoot)  // set content root
              .UseWebRoot(webRoot)          // set web root
              .UseStartup<Startup>();
        }

        public static void Shutdown()
        {
            cancelTokenSource.Cancel();
        }
    }
}
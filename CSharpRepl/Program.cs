﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace CSDiscordService
{
    public class Program
    {
        public static async Task Main()
        {
            Environment.SetEnvironmentVariable("HOME", Path.GetTempPath());
            var host = CreateWebHostBuilder().Build();

            await host.RunAsync();
        }

        public static IWebHostBuilder CreateWebHostBuilder() =>
            WebHost.CreateDefaultBuilder()
                .ConfigureLogging((context, builder) =>
                {
                    builder.ClearProviders();
                    builder.AddConfiguration(context.Configuration);
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        builder.AddDebug();
                    }
                    if (context.HostingEnvironment.IsProduction())
                    {
                        builder.AddSeq("http://seq:5341");
                    }

                    builder.AddSimpleConsole(o =>
                    {
                        o.ColorBehavior = LoggerColorBehavior.Disabled;
                        o.SingleLine = true;
                        o.TimestampFormat = "yyyy-MM-ddThh:mm:ss.zzzz ";
                        o.UseUtcTimestamp = true;
                    });
                    builder.SetMinimumLevel(LogLevel.Trace);
                    builder.AddFilter(level => true);
                })
                .UseStartup<Startup>();
    }
}

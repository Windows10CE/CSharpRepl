﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using CSDiscordService.Eval;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using CSDiscordService.Infrastructure.JsonFormatters;
using System.Text.Json.Serialization;

namespace CSDiscordService
{
    public class Startup
    {
        private readonly Timer _exitTimer;
        private  IHostApplicationLifetime _appLifetime;

        public Startup(IConfiguration config)
        {
            _exitTimer = new Timer((s) => _appLifetime?.StopApplication(), null, Timeout.Infinite, Timeout.Infinite);
            Configuration = config;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<CSharpEval>();
            services.AddSingleton<DisassemblyService>();
            var jsonOptions = new JsonSerializerOptions
            {
                MaxDepth = 10240,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                Converters = { 
                    new TypeJsonConverter(), new TypeInfoJsonConverter(),
                    new RuntimeTypeHandleJsonConverter(), new TypeJsonConverterFactory(), new AssemblyJsonConverter(),
                    new ModuleJsonConverter(), new AssemblyJsonConverterFactory(), 
                    new DirectoryInfoJsonConverter(),
                     new AngouriMathEntityConverter(), new AngouriMathEntityVarsConverter(),
                    new IntPtrJsonConverter() 
                    }
            };

            services.AddControllers(o =>
            {
                o.RespectBrowserAcceptHeader = true;
                o.InputFormatters.Clear();
                o.InputFormatters.Insert(0, new PlainTextInputFormatter());
            }).AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.MaxDepth = 10240;
                o.JsonSerializerOptions.IncludeFields = true;
                o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                o.JsonSerializerOptions.Converters.Add(new TypeJsonConverter());
                o.JsonSerializerOptions.Converters.Add(new TypeInfoJsonConverter());
                o.JsonSerializerOptions.Converters.Add(new RuntimeTypeHandleJsonConverter());
                o.JsonSerializerOptions.Converters.Add(new TypeJsonConverterFactory());
                o.JsonSerializerOptions.Converters.Add(new AssemblyJsonConverter());
                o.JsonSerializerOptions.Converters.Add(new ModuleJsonConverter());
                o.JsonSerializerOptions.Converters.Add(new AssemblyJsonConverterFactory());
                o.JsonSerializerOptions.Converters.Add(new DirectoryInfoJsonConverter());
                o.JsonSerializerOptions.Converters.Add(new AngouriMathEntityConverter());
                o.JsonSerializerOptions.Converters.Add(new AngouriMathEntityVarsConverter());
                o.JsonSerializerOptions.Converters.Add(new IntPtrJsonConverter());
            });
            services.AddSingleton(jsonOptions);

            services.AddTransient<IPreProcessorService, DefaultPreProcessorService>();
            services.AddTransient<IDirectiveProcessor, NugetDirectiveProcessor>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, CSharpEval evalService, IHostApplicationLifetime appLifetime)
        {
            _appLifetime = appLifetime;
            // run eval once on startup so the first time its hit isn't cripplingly slow.
            evalService.RunEvalAsync("1+1").ConfigureAwait(false).GetAwaiter().GetResult();
            app.UseRouting();
            app.Use(async (context, next) =>
            {
                if (env.IsProduction() && !context.Response.HasStarted && (context.Request.Path.Equals("/eval", StringComparison.OrdinalIgnoreCase)))
                {
                    // terminate hte process after 30 seconds whether the request is done or not (infinite loops, long sleeps, etc)
                    _exitTimer.Change(TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);

                    // terminate the process when the rquest finishes (assume the code is malicious. 
                    // Should be hosted in a container/host system that destroys/re-builds the container)
                    context.Response.OnCompleted(async () =>
                    {
                        await Task.Delay(5000);
                        _appLifetime.StopApplication();
                    });
                }

                await next();
            });

            app.UseEndpoints(o =>
            {
                o.MapControllers();
            });
        }
    }
}

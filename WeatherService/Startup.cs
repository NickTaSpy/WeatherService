﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WeatherService.Error;
using WeatherService.Security;
using WeatherService.WeatherProviders;

namespace WeatherService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Configure strongly typed settings objects.
            services.Configure<ApiKeys>(Configuration.GetSection("Keys"));

            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            var securityConfigSection = Configuration.GetSection("SecurityConfig");
            services.Configure<SecurityConfig>(securityConfigSection);

            // Configure JWT authentication.
            var securityConfig = securityConfigSection.Get<SecurityConfig>();
            var key = Encoding.ASCII.GetBytes(securityConfig.Secret);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = true;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            // MemoryCache.
            var appSettings = appSettingsSection.Get<AppSettings>();
            services.AddMemoryCache(options =>
            {
                options.ExpirationScanFrequency = TimeSpan.FromSeconds(appSettings.CacheExpirationScanInterval);
                options.SizeLimit = appSettings.CacheSizeLimit;
            });

            // Configure DI for application services.
            services.AddScoped<UserService>();
            services.AddSingleton<WeatherProviderManager>();
            services.AddMvc(options =>
            {
                options.CacheProfiles.Add("DefaultWeather",
                    new CacheProfile()
                    {
                         Duration = 3600
                    });
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/json";

                        //var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                        var errorInfo = new ErrorInfo(context.Response.StatusCode, "An unexpected exception occured.");

                        await context.Response.WriteAsync(errorInfo.ToString());
                    });
                });

                app.UseHsts();
            }

            app.UseAuthentication();
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}

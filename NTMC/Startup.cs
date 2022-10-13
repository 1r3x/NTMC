using NTMC.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiAccessLibrary;
using ApiAccessLibrary.Implementation;
using ApiAccessLibrary.Interfaces;
using DataAccessLibrary.Implementation;
using DataAccessLibrary.Interfaces;
using EntityModelLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using EntityModelLibrary.ViewModels;
using NTMC.CryptoGraphy;

namespace NTMC
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            //todo experiment 
            //
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<WeatherForecastService>();
            //this context is used for test environment 
            services.AddDbContext<DbContextForTest>(
                options => options.UseSqlServer("name=ConnectionStrings:TestEnvironmentConnection"));
            //this context is used for prod_old environment 
            services.AddDbContext<DbContextForProdOld>(
                options => options.UseSqlServer("name=ConnectionStrings:ProdOldConnection"));
            //this context is used for prod environment 
            services.AddDbContext<DbContextForProd>(
                options => options.UseSqlServer("name=ConnectionStrings:ProdConnection"));
            //for centralize data 
            services.Configure<CentralizeVariablesModel>(Configuration.GetSection("CentralizeVariables"));
            //DI
            services.AddHttpClient<IProcessSaleTransactions, ProcessSaleTransactions>();
            services.AddHttpClient<IProcessCardAuthorization, ProcessCardAuthorization>();
            services.AddScoped<IAddNotes, AddNotes>();
            services.AddScoped<IPopulateDataForProcessSales, PopulateDataForProcessSales>();
            services.AddScoped<IPayment, Payment>();
            services.AddScoped<IAddCardInfo, AddCardInfo>();
            services.AddScoped<IAddPaymentSchedule, AddPaymentSchedule>();
            services.AddScoped<IAddCcPayment, AddCcPayment>();
            services.AddScoped<IGetPreSchedulePaymentInfo, GetPreSchedulePaymentInfo>();
            services.AddScoped<IGetDetailsOfPreSchedulePayment, GetDetailsOfPreSchedulePayment>();
            services.AddScoped<IAddPaymentScheduleHistory, AddPaymentScheduleHistory>();
            services.AddScoped<IiProGatewayServices, IProGatewayServices>();
            services.AddScoped<ICryptoGraphy, Cryptography>();
            //state managements 
            services.AddScoped<StateContainer>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}

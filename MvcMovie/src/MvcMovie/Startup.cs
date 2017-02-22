using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MvcMovie.Data;
using MvcMovie.Models;
using MvcMovie.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace MvcMovie
{
	public class Startup
	{
		public Startup(IHostingEnvironment env)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

			if (env.IsDevelopment())
			{
				// For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
				builder.AddUserSecrets();
			}

			builder.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{

			services.AddAuthorization(options =>
			{
				options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));
			});

			// Add framework services.
			services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

			// Configure Identity
			services.Configure<IdentityOptions>(options =>
			{

				// Password settings
				options.Password.RequireDigit = true;
				options.Password.RequiredLength = 8;
				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequireUppercase = true;
				options.Password.RequireLowercase = false;

				// Lockout settings
				options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
				options.Lockout.MaxFailedAccessAttempts = 5;

				// Cookie settings
				options.Cookies.ApplicationCookie.ExpireTimeSpan = TimeSpan.FromDays(150);
				options.Cookies.ApplicationCookie.LoginPath = "/Account/LogIn";
				options.Cookies.ApplicationCookie.LogoutPath = "/Account/LogOff";
				options.Cookies.ApplicationCookie.AccessDeniedPath = "/Account/Forbidden/";
				options.Cookies.ApplicationCookie.AutomaticAuthenticate = true;
				options.Cookies.ApplicationCookie.AutomaticChallenge = true;
				options.Cookies.ApplicationCookie.AuthenticationScheme = "Cookie";

				// User settings
				options.User.RequireUniqueEmail = true;
			});

			services.AddIdentity<ApplicationUser, IdentityRole>()
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddDefaultTokenProviders();

			services.AddMvc(options =>
			{
				options.SslPort = 44321;
				options.Filters.Add(new RequireHttpsAttribute());
			});

			

			// Add application services.
			services.AddTransient<IEmailSender, AuthMessageSender>();
			services.AddTransient<ISmsSender, AuthMessageSender>();


		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseDatabaseErrorPage();
				app.UseBrowserLink();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
			}

			app.UseStaticFiles();

			app.UseIdentity();

			// Add external authentication middleware below. To configure them please see http://go.microsoft.com/fwlink/?LinkID=532715
			//Facebook
			app.UseFacebookAuthentication(new FacebookOptions()
			{
				AppId = Configuration["Authentication:Facebook:AppId"],
				AppSecret = Configuration["Authentication:Facebook:AppSecret"]
			});

			//Google
			app.UseGoogleAuthentication(new GoogleOptions()
			{
				ClientId = Configuration["Authentication:Google:ClientId"],
				ClientSecret = Configuration["Authentication:Google:ClientSecret"]
			});

			app.UseMvc(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{id?}");
			});

			SeedData.Initialize(app.ApplicationServices);
			var serviceProvider = app.ApplicationServices.GetService<IServiceProvider>(); CreateRoles(serviceProvider).Wait();


		}


		private async Task CreateRoles(IServiceProvider serviceProvider)
		{
			var RoleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
			string[] roleNames = { "Administrator", "Customer" };
			IdentityResult roleResult;

			foreach (var roleName in roleNames)
			{
				//If we already have this role, else
				var roleExist = await RoleManager.RoleExistsAsync(roleName);
				if (!roleExist)
				{
					//create the roles and seed them to the database.
					roleResult = await RoleManager.CreateAsync(new IdentityRole(roleName));
				}
			}
		}
	}
}

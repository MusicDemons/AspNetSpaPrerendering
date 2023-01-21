using Demo.Data.Dal.Services;
using Demo.Data.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MintPlayer.AspNetCore.SpaServices.Prerendering;
using MintPlayer.AspNetCore.SpaServices.Routing;
using System.Linq;
using System.Threading.Tasks;
using WebMarkupMin.AspNetCore6;

namespace Demo.Web;

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
		services.AddControllersWithViews();

		services
			.AddDemo(options =>
			{
				options.ConnectionString = Configuration.GetConnectionString("Demo");
			})
			.AddSpaStaticFiles(configuration =>
			{
				configuration.RootPath = "ClientApp/dist";
			});

		// Define the SPA-routes for our helper
		services.AddSpaPrerenderingService<Services.DemoSpaPrerenderingService>();
		services.AddWebMarkupMin().AddHttpCompression().AddHtmlMinification();

		services
			.Configure<RazorViewEngineOptions>(options =>
			{
				var new_locations = options.ViewLocationFormats.Select(vlf => $"/Server{vlf}").ToList();
				options.ViewLocationFormats.Clear();
				foreach (var format in new_locations)
					options.ViewLocationFormats.Add(format);
			})
			.Configure<WebMarkupMinOptions>(options =>
			{
				options.DisablePoweredByHttpHeaders = true;
				options.AllowMinificationInDevelopmentEnvironment = true;
				options.AllowCompressionInDevelopmentEnvironment = true;
				options.DisablePoweredByHttpHeaders = false;
			})
			.Configure<HtmlMinificationOptions>(options =>
			{
				options.MinificationSettings.RemoveEmptyAttributes = true;
				options.MinificationSettings.RemoveRedundantAttributes = true;
				options.MinificationSettings.RemoveHttpProtocolFromAttributes = true;
				options.MinificationSettings.RemoveHttpsProtocolFromAttributes = false;
				options.MinificationSettings.MinifyInlineJsCode = true;
				options.MinificationSettings.MinifyEmbeddedJsCode = true;
				options.MinificationSettings.MinifyEmbeddedJsonData = true;
				options.MinificationSettings.WhitespaceMinificationMode = WebMarkupMin.Core.WhitespaceMinificationMode.Aggressive;
			});
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
		if (!env.IsDevelopment())
		{
			app.UseSpaStaticFiles();
		}

		app.UseRouting();

		app.UseEndpoints(endpoints =>
		{
			endpoints.MapControllerRoute(
				name: "default",
				pattern: "{controller}/{action=Index}/{id?}");
		});

		app.UseSpa(spa =>
		{
			// To learn more about options for serving an Angular SPA from ASP.NET Core,
			// see https://go.microsoft.com/fwlink/?linkid=864501

			spa.Options.SourcePath = "ClientApp";

			//spa.ApplicationBuilder.UseResponseCaching().UseHsts();
			spa.UseSpaPrerendering(options =>
			{
				options.BootModulePath = $"{spa.Options.SourcePath}/dist/ClientApp/server/main.js";
				//options.BootModuleBuilder = env.IsDevelopment() ? new AngularPrerendererBuilder(npmScript: "build:ssr:development") : null;
				options.ExcludeUrls = new[] { "/sockjs-node" };

				options.OnPrepareResponse = (context) =>
				{
					context.Response.Headers.Add("Whatever", "Oasis");
					return Task.CompletedTask;
				};
			});

			app.UseWebMarkupMin();

			if (env.IsDevelopment())
			{
				spa.UseAngularCliServer(npmScript: "start");
			}
		});
	}
}

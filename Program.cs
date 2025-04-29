using MedManager.Data;
using MedManager.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Sinks.File;


var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
	.ReadFrom.Configuration(new ConfigurationBuilder()
	.AddJsonFile("appsettings.json")
	.Build())
	.CreateLogger();

builder.Services.AddControllersWithViews();

var serverVersion = new MySqlServerVersion(new Version(10, 4, 32));

builder.Services.AddDbContext<ApplicationDbContext>(
	options => options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), serverVersion)
);

builder.Services.AddIdentity<Medecin, IdentityRole>(options =>
{
	options.SignIn.RequireConfirmedAccount = false;
	options.Password.RequireDigit = false;
	options.Password.RequireLowercase = true;
	options.Password.RequireNonAlphanumeric = false;
	options.Password.RequireUppercase = false;
	options.Password.RequiredLength = 4;

	options.User.RequireUniqueEmail = true;

}).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Compte/Connexion";
    options.AccessDeniedPath = "/Compte/Connexion";
    
});

builder.Services.AddLogging(loggingBuilder =>
{
	loggingBuilder.AddSerilog(); 
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
else
{
	app.UseExceptionHandler("/Error/Index");
	app.UseStatusCodePagesWithRedirects("/Error/Index");
}



app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Accueil}/{action=Index}/{id?}");

app.Run();

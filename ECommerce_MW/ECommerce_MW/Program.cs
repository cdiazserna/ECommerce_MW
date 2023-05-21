using ECommerce_MW.DAL;
using ECommerce_MW.DAL.Entities;
using ECommerce_MW.Helpers;
using ECommerce_MW.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using System.Globalization;
using System.Text.Json.Serialization;
using Vereyon.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddDbContext<DatabaseContext>(o =>
{
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddIdentity<User, IdentityRole>(io =>
{
    io.User.RequireUniqueEmail = true; //Valida que el email no esté repetido
    io.Password.RequireDigit = false;
    io.Password.RequiredUniqueChars = 0;
    io.Password.RequireLowercase = false;
    io.Password.RequireNonAlphanumeric = false;
    io.Password.RequireUppercase = false;
    io.Password.RequiredLength = 6;

}).AddEntityFrameworkStores<DatabaseContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/Account/Unauthorized";
	options.AccessDeniedPath = "/Account/Unauthorized";
});

var supportedCultures = new[]
{
    new CultureInfo("es-CO"),
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("es-CO");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddFlashMessage();
builder.Services.AddTransient<SeederDb>();
builder.Services.AddScoped<IUserHelper, UserHelper>();
builder.Services.AddScoped<IDropDownListsHelper, DropDownListsHelper>();
builder.Services.AddScoped<IAzureBlobHelper, AzureBlobHelper>();
builder.Services.AddScoped<IOrderHelper, OrderHelper>();

var app = builder.Build();

app.UseRequestLocalization();

SeederData();
void SeederData()
{
    IServiceScopeFactory? scopedFactory = app.Services.GetService<IServiceScopeFactory>();

    using (IServiceScope? scope = scopedFactory.CreateScope())
    {
        SeederDb? service = scope.ServiceProvider.GetService<SeederDb>();
        service.SeederAsync().Wait();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

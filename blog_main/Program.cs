using blog_main.Data;
using blog_main.Profiles;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAutoMapper(typeof(AppMappingProfile));

builder.Services.AddMinio(options =>
{
  options.Endpoint = "localhost:9000";
  options.AccessKey = "deoma";
  options.SecretKey = "fatfarm1";
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<BlogDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IPostRepo, PostRepo>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Blog}/{action=Index}/{id?}");

app.Run();
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PodcastManagementSystem.Data;
using PodcastManagementSystem.Interfaces;
using PodcastManagementSystem.Repositories;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleSystemsManagement;
using PodcastManagementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------
// 1. Configuration and Connection String Retrieval
// ----------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// ----------------------------------------------------
// 2. Add services to the container (Core ASP.NET Identity/MVC)
// ----------------------------------------------------
// a. DbContext Registration (Only one time!)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// b. Identity Configuration
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// c. MVC/Controllers
builder.Services.AddControllersWithViews();


// ----------------------------------------------------
// 3. AWS Service Registration
// ----------------------------------------------------
// a. Configure AWS Options 
var awsOptions = builder.Configuration.GetAWSOptions();
builder.Services.AddDefaultAWSOptions(awsOptions);

// b. Register AWS Clients (S3, DynamoDB, Parameter Store)
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddAWSService<IAmazonSimpleSystemsManagement>(); \

// c. Register DynamoDB Context
builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();

// d. Register Repositories and Services
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IS3Service, S3Service>();             
builder.Services.AddScoped<IParameterStoreService, ParameterStoreService>(); 
// ----------------------------------------------------


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
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
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------
// 1. Configuration and Connection String Retrieval
// ----------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// ----------------------------------------------------
// 2. Add services to the container (Core ASP.NET Identity/MVC)
// ----------------------------------------------------
// a. DbContext Registration 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();


// b. Identity Configuration 

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI() // Keep this here to register the Identity Pages
.AddDefaultTokenProviders();

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
builder.Services.AddAWSService<IAmazonSimpleSystemsManagement>();


// c. Register DynamoDB Context
builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();

// d. Register Repositories and Services
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IPodcastRepository, PodcastRepository>();
builder.Services.AddScoped<IS3Service, S3Service>();             
builder.Services.AddScoped<IParameterStoreService, ParameterStoreService>(); 
// ----------------------------------------------------


var app = builder.Build();

// Adding roles 
//using (var scope = app.Services.CreateScope())
//{
//    var serviceProvider = scope.ServiceProvider;
//    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

//    // Required roles: Podcaster, Listener/viewer, Admin
//    string[] roleNames = { "Admin", "Podcaster", "Listener/viewer" };

//    foreach (var roleName in roleNames)
//    {
//        // Check if the role already exists asynchronously
//        if (!roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
//        {
//            // If the role doesn't exist, create it
//            roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
//        }
//    }
//}

await CreateRolesAsync(app);

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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();












static async Task CreateRolesAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var serviceProvider = scope.ServiceProvider;
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roleNames = { "Admin", "Podcaster", "Listener/viewer" };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}
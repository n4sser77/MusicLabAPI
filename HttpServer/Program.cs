


using Backend;
using Backend.asp.Services.Interfaces;
using HttpServer.asp.Services;
using HttpServer.asp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MusiclabApi;
using MusiclabApi.Settings;
using Microsoft.Extensions.Options;
using Supabase;
using MusiclabApi.Services;




var builder = WebApplication.CreateBuilder(args);





// Optionally, add services to the container here
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserManager, UserManager>();
builder.Services.AddScoped<IStorageProvider, SupabaseBlob>();
builder.Services.Configure<SupabaseSettings>(builder.Configuration.GetSection("SupabaseSettings"));

builder.Services.AddSingleton(provider =>
{
    var supabaseSettings = provider.GetRequiredService<IOptions<SupabaseSettings>>().Value;
    Console.WriteLine("supabase url fetched: " +  supabaseSettings.Url);
    if (supabaseSettings is null) throw new ArgumentNullException();
    // If Supabase.Client has a constructor that accepts the settings object, pass it.
    // Keep this line consistent with the Supabase client API you use.
    return new Supabase.Client(supabaseSettings.Url, supabaseSettings.ServiceKey, new SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = true
    });
});

// For example, if you plan to use controllers:
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(op =>
                {
                    op.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = "MusicLabBackEndApi",
                        ValidAudience = "MusicLabAppUsers",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperSecretKey_ChangeThislater123456789"))

                    };
                });



builder.Services.AddAuthorization();

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      builder =>
                      {
                          builder.WithOrigins("http://localhost:5173", "http://localhost:8080", "https://sonic-locker.netlify.app")
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                      });
});




builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddTransient<IWaveformGeneratorService, WaveformServiceSkia>();
// Read the secret key from configuration
var signedUrlConfig = builder.Configuration.GetSection("SignedUrl");
var secretKey = signedUrlConfig.GetValue<string>("SecretKey");

builder.Services.AddSingleton(new SignedUrlService(secretKey));

// 🔹 Database setup depending on environment
//if (builder.Environment.IsDevelopment())
//{
//    // SQL Server in dev
//    builder.Services.AddDbContext<AppDbContext>(options =>
//        options.UseSqlServer(@"Server=.\SQLEXPRESS;Database=FileUploadDEMO;
//                                Trusted_Connection=True;TrustServerCertificate=True;
//                                Integrated Security=True"));
//}
//else
//{
// SQLite in production

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var supabaseConnStr = builder.Configuration.GetConnectionString("Supabase");

Console.WriteLine("Using SQLite with connection string: " + supabaseConnStr);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDbContext<PostgresAppDb>(op => op.UseNpgsql(supabaseConnStr));



//}





builder.Services.AddControllers();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();


    db.Database.Migrate(); // applies only if there are pending migrations

}


app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();



// Configure the HTTP request pipeline.
// If you added controllers, map them:
app.MapControllers();



// Alternatively, for a simple endpoint, you can add:
app.MapGet("/", () => "Hello from the empty ASP.NET Core HTTP Server!");


app.Run();


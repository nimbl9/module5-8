using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Asp.Versioning;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using module1_4.Common;
using module1_4.Data;
using module1_4.Mapping;
using module1_4.Middleware;
using module1_4.Models;
using module1_4.Repositories;
using module1_4.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EduCampusContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("CampusDb");
    options.UseSqlServer(connectionString);
});

builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<EduCampusContext>()
.AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "CampusUniversitySecureKeyForJwtTokens2026SuperSecretString!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EduCampusApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EduCampusClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            var packet = ApiResponsePacket<object>.Failure(
                StatusCodes.Status401Unauthorized,
                "UNAUTHORIZED",
                "доступ запрещен: требуется действующий jwt-токен аутентификации",
                traceId);

            var json = JsonSerializer.Serialize(packet, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(json);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            var packet = ApiResponsePacket<object>.Failure(
                StatusCodes.Status403Forbidden,
                "FORBIDDEN",
                "доступ отклонен: недостаточно прав для выполнения данной операции",
                traceId);

            var json = JsonSerializer.Serialize(packet, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(json);
        }
    };
});

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:5050", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("UniversityClientPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

var mapperConfig = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<AppMappingProfile>();
});
builder.Services.AddSingleton(mapperConfig.CreateMapper());

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IStudentStore, StudentStore>();
builder.Services.AddScoped<ITeacherStore, TeacherStore>();
builder.Services.AddScoped<ICourseStore, CourseStore>();
builder.Services.AddScoped<IEnrollmentStore, EnrollmentStore>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var aggregatedErrors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(msg => !string.IsNullOrWhiteSpace(msg))
                .ToList();

            var combinedText = string.Join("; ", aggregatedErrors);
            var currentTraceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

            var packet = ApiResponsePacket<object>.Failure(
                StatusCodes.Status400BadRequest,
                "VALIDATION_ERROR",
                string.IsNullOrWhiteSpace(combinedText) ? "переданы некорректные данные" : combinedText,
                currentTraceId);

            return new BadRequestObjectResult(packet);
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "University Course Management API",
        Version = "v1",
        Description = "Базовая версия REST Web API с поддержкой CRUD и ролевой модели"
    });

    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "University Course Management API",
        Version = "v2",
        Description = "Расширенная версия API с дополнительными метриками курсов"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите полученный JWT-токен"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionCatchMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "University API v1.0");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "University API v2.0");
    });
}

app.UseHttpsRedirection();
app.UseCors("UniversityClientPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EduCampusContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await CampusDbSeeder.SeedAsync(context, userManager, roleManager, logger);
}

app.Logger.LogInformation("модернизированный защищенный веб-сервис успешно запущен");

app.Run();
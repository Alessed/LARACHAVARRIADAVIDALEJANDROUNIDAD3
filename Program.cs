using Microsoft.EntityFrameworkCore;
using Dentalara.Data;
using Dentalara.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// =============================
// 1. Autenticación con cookies
// =============================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

// =============================
// 2. Autorización por roles
// =============================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("DentistaOnly", policy => policy.RequireRole("Dentista"));
});

// =============================
// 3. Sesiones
// =============================
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "Dentalara.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Para localhost
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// =============================
// 4. MVC + EF Core
// =============================
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =============================
// 5. reCAPTCHA
// =============================
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<GoogleRecaptchaConfig>(builder.Configuration.GetSection("GoogleRecaptcha"));
builder.Services.AddHttpClient<RecaptchaService>(client =>
{
    client.BaseAddress = new Uri("https://www.google.com/recaptcha/api/siteverify");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

// =============================
// 6. Email (opcional)
// =============================
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

var app = builder.Build();

// =============================
// 7. Manejo de errores
// =============================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// =============================
// 8. Seguridad - No cache
// =============================
app.Use(async (context, next) =>
{
    context.Response.GetTypedHeaders().CacheControl =
        new Microsoft.Net.Http.Headers.CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true,
            MustRevalidate = true
        };
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "-1";

    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// =============================
// 9. Autenticación y sesión
// =============================
app.UseSession();
app.UseAuthentication();

// =============================
// 10. Middleware Multisesión
// =============================
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var sessionTokenClaim = context.User.FindFirst("SessionToken")?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && !string.IsNullOrEmpty(sessionTokenClaim))
        {
            using var connection = new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            using var cmd = new SqlCommand("SELECT SessionToken FROM Usuarios WHERE IdUsuario = @IdUsuario", connection);
            cmd.Parameters.AddWithValue("@IdUsuario", int.Parse(userIdClaim));
            var dbToken = (string)await cmd.ExecuteScalarAsync();

            if (dbToken != sessionTokenClaim)
            {
                // Logout forzado si la sesión fue reemplazada
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                context.Session.Clear();
                context.Response.Redirect("/Auth/Login");
                return;
            }
        }
    }

    await next();
});

app.UseAuthorization();

// =============================
// 11. Ruta por defecto
// =============================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

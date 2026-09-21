using LocaSmart360.Data;
using LocaSmart360.Repositories;
using LocaSmart360.Repositories.Interfaces;
using LocaSmart360.Services;
using LocaSmart360.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Banco de Dados (Lendo corretamente do secrets.json / SupabaseConnection)
var connectionString = builder.Configuration.GetConnectionString("SupabaseConnection");
builder.Services.AddDbContext<LocaSmartDbContext>(options =>
    options.UseNpgsql(connectionString));

// Registrar o Acessor de Contexto HTTP
builder.Services.AddHttpContextAccessor();

// Adicionar Serviços de Sessão
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Registrar suporte MVC
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

// Registrar os Repositórios do Projeto
builder.Services.AddScoped<IVendaRepository, VendaRepository>();

// Registrar o serviço de Inteligência de Fraude
builder.Services.AddScoped<AnaliseFraudeService>();

// Registrar o motor de ETL
builder.Services.AddScoped<IEtlService, EtlService>();

var app = builder.Build();

// Pipeline de requisições HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Autenticacao}/{action=Login}/{id?}");

app.Run();
using BlazorAppJobManager.Components;
using BlazorAppJobManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<JobService>();

// ���K�[�̒ǉ�
builder.Logging.SetMinimumLevel(LogLevel.Debug); // ���O���x�����w��
builder.Logging.AddConsole(); // �R���\�[���Ƀ��O���o��
builder.Logging.AddDebug(); // �f�o�b�O�Ƀ��O���o��

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

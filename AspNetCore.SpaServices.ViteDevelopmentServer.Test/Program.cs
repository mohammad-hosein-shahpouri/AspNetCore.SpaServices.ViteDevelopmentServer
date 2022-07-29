using AspNetCore.SpaServices.ViteDevelopmentServer;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSpaStaticFiles(configuration => configuration.RootPath = "ClientApp/build");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpsRedirection();

app.UseSpaStaticFiles();

app.UseAuthorization();

app.UseEndpoints(endpoints => endpoints.MapControllers());
app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";

    if (isDevelopment)
        spa.UseViteDevelopmentServer(npmScript: "dev");
});
app.Run();
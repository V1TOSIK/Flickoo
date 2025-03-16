using Flickoo.Api.Data;
using Flickoo.Api.Interfaces;
using Flickoo.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();

builder.Services.ConfigureTelegramBotMvc();


builder.Services.AddDbContext<FlickooDbContext>();

builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

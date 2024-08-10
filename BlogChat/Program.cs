using BlogChat.WebSocket;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseWebSockets(); 

app.Map("/ws/chat", (HttpContext context) =>
{
    var webSocketHandler = context.RequestServices.GetRequiredService<WebSocketHandler>();
    return webSocketHandler.HandleWebSocketConnections(context);
});

app.MapControllers();

app.Run();

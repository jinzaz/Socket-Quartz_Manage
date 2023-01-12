using Quartz.Impl;
using Quartz.Spi;
using Quartz;
using Socket_Quartz_Manage.Quartz;
using Socket_Quartz_Manage.SocketBase.SocketClient;
using Socket_Quartz_Manage.SocketBase.SocketService;
using Socket_Quartz_Manage.Quartz.Jobs;

namespace Socket_Quartz_Manage
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            #region Socket
            builder.Services.AddSingleton<ISocketServerManager, SocketServerManager>();
            builder.Services.AddHostedService<SocketBackground>();

            builder.Services.AddSingleton<SocketClientBackground>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<SocketClientBackground>());
            builder.Services.AddSingleton<ISocketClientManager>(sp => sp.GetRequiredService<SocketClientBackground>());
            #endregion

            #region Quartz
            builder.Services.AddTransient<DefaultJob>();
            builder.Services.AddTransient<TestJob>();

            builder.Services.AddSingleton<IJobFactory, DefaultJobFactory>();
            builder.Services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();

            builder.Services.AddSingleton<QuartzBackground>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<QuartzBackground>());
            builder.Services.AddSingleton<IQuartzBackground>(sp => sp.GetRequiredService<QuartzBackground>());

            #endregion

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
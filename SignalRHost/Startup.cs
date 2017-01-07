namespace SignalRHost
{
    #region Using

    using System;
    using System.Threading;
    using Microsoft.Owin;
    using Microsoft.Owin.Cors;
    using Microsoft.Owin.FileSystems;
    using Microsoft.Owin.StaticFiles;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Owin;
    using ScaleOut;

    #endregion

    public static class Startup
    {
        public static void ConfigureApp(IAppBuilder app)
        {
            Thread.Sleep(TimeSpan.FromSeconds(60));
            ConfigureCors(app);
            ConfigureSignalR(app);
            RegisterScaleOut();
            ConfigureStaticFiles(app);

            ServiceEventSource.Current.Message("App configuration finished");
        }

        private static async void RegisterScaleOut()
        {
            var hostName = Environment.MachineName;
            var endpoint = $"http://{hostName}:8322/int/";
            var so = ServiceProxy.Create<IScaleOut>(new Uri(SignalRHostConfiguration.SFScaleoutConnectionString), new ServicePartitionKey(1));
            await so.RegisterSignalRHost(hostName, endpoint);
            ServiceEventSource.Current.Message($"{hostName} registered with Scaleout service");
        }

        private static void ConfigureCors(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
        }

        private static void ConfigureSignalR(IAppBuilder app)
        {
            app.MapSignalR<ExternalConnection>("/ext");
            app.MapSignalR<InternalConnection>("/int");
        }


        private static void ConfigureStaticFiles(IAppBuilder app)
        {
            var fileSystem = new PhysicalFileSystem(@".\wwwroot");
            var options = new FileServerOptions
            {
                EnableDefaultFiles = true,
                RequestPath = PathString.Empty,
                FileSystem = fileSystem
            };

            options.DefaultFilesOptions.DefaultFileNames = new[] {"index.html"};
            options.StaticFileOptions.FileSystem = fileSystem;

            app.UseFileServer(options);
        }
    }
}
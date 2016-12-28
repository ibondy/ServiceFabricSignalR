using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Owin;
using ScaleOut;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;
using System.Diagnostics;
using System.Collections.Generic;

namespace SignalRHost
{
    public static class Startup
    {
        public static void ConfigureApp(IAppBuilder app)
        {
            ConfigureCors(app);
            ConfigureSignalR(app);
            ////ConfigureStaticFiles(app);

            //RunIncrementingTask();
            //MessageLoop();
            RegisterScaleOut();
            Test();
           
        }

        private static async void Test()
        {
           await Task.Delay(TimeSpan.FromSeconds(30));
            IMessageRouter mr = ServiceProxy.Create<IMessageRouter>(new Uri("fabric:/SFScaleOut/ScaleOut"), new ServicePartitionKey(0));
            await mr.BroadcastAsync("Hello from SignalR host");
        }

        private static async void RegisterScaleOut()
        {
            
            var hostName = Environment.MachineName;
            var endpoint = $"http://{hostName}:8322/int/";
            IScaleOut so = ServiceProxy.Create<IScaleOut>(new Uri(SignalRScaleoutConfiguration.SFConnectionString), new ServicePartitionKey(0));
            await so.RegisterSignalRHost(hostName, endpoint);
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

        //private static void ConfigureStaticFiles(IAppBuilder app)
        //{
        //    var fileSystem = new PhysicalFileSystem(@".\wwwroot");
        //    var options = new FileServerOptions
        //    {
        //        EnableDefaultFiles = true,
        //        RequestPath = PathString.Empty,
        //        FileSystem = fileSystem,
        //    };

        //    options.DefaultFilesOptions.DefaultFileNames = new[] { "index.html" };
        //    options.StaticFileOptions.FileSystem = fileSystem;

        //    app.UseFileServer(options);
        //}

        private static void RunIncrementingTask()
        {
            Task.Factory.StartNew(async () =>
            {
                var random = new Random();

                while (true)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(random.Next(500, 2000)));

                    var message = $"Hello from node {FabricRuntime.GetNodeContext().NodeId}";

                    await GlobalHost.ConnectionManager.GetConnectionContext<ExternalConnection>().Connection.Broadcast(message);

                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private static async void MessageLoop()
        {
            await Task.Delay(TimeSpan.FromSeconds(10));   
            await Task.Factory.StartNew(async () =>
             {
                 var messages = new List<string>();
                 while (true)
                 {
                     await Task.Delay(TimeSpan.FromMilliseconds(10));
                     IScaleOut so = ServiceProxy.Create<IScaleOut>(new Uri("fabric:/SFScaleOut/ScaleOut"), new ServicePartitionKey(0));
                     messages.AddRange(await so.GetMessages(10));

                     Debug.WriteLine(messages.Count);

                  

                 }
             }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                
        }
    }
}

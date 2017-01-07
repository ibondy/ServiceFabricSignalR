namespace EchoService
{
    #region Using

    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading.Tasks;
    using System.Timers;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using ScaleOut;

    #endregion

    public interface IEchoService : IService
    {
        Task Echo(string host, string connectionId, string message);

        Task Broadcast(string message);
    }

    /// <summary>
    ///     An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class EchoService : StatelessService, IEchoService
    {
        public EchoService(StatelessServiceContext context) : base(context)
        {
            Timer = new Timer(10000);
            Timer.Elapsed += Timer_Elapsed;
            Timer.Enabled = true;
        }

        #region

        private Timer Timer { get; }

        #endregion

        public async Task Broadcast(string message)
        {
            var proxy = ServiceProxy.Create<IMessageRouter>(new Uri("fabric:/SFScaleOut/ScaleOut"), new ServicePartitionKey(0));
            await proxy.BroadcastAsync($"Echo service broadcast :{message}").ConfigureAwait(false);
        }

        public async Task Echo(string host, string connectionId, string message)
        {
            var proxy = ServiceProxy.Create<IMessageRouter>(new Uri("fabric:/SFScaleOut/ScaleOut"), new ServicePartitionKey(0));
            await proxy.SendAsync(host, connectionId, $"Echo service response :{message}").ConfigureAwait(false);
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Broadcast($"Hello from Echo Service timer {DateTime.Now.Millisecond}").ConfigureAwait(false);
        }

        /// <summary>
        ///     Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[] {new ServiceInstanceListener(this.CreateServiceRemotingListener)};
        }
    }
}
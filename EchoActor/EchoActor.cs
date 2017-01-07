namespace EchoActor
{
    #region Using

    using System;
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using ScaleOut;

    #endregion

    /// <remarks>
    ///     This class represents an actor.
    ///     Every ActorID maps to an instance of this class.
    ///     The StatePersistence attribute determines persistence and replication of actor state:
    ///     - Persisted: State is written to disk and replicated.
    ///     - Volatile: State is kept in memory only and replicated.
    ///     - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Volatile)]
    internal class EchoActor : Actor, IEchoActor
    {
        /// <summary>
        ///     Initializes a new instance of EchoActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public EchoActor(ActorService actorService, ActorId actorId) : base(actorService, actorId)
        {
        }

        #region

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private IActorTimer EchoTimer { get; set; }

        #endregion

        public async Task Broadcast(string message)
        {
            var proxy = ServiceProxy.Create<IMessageRouter>(new Uri("fabric:/SFScaleOut/ScaleOut"), new ServicePartitionKey(0));
            await proxy.BroadcastAsync($"Echo actor broadcast :{message} {DateTime.Now.Millisecond}").ConfigureAwait(false);
        }

        public async Task Echo(string host, string connectionId, string message)
        {
            var proxy = ServiceProxy.Create<IMessageRouter>(new Uri("fabric:/SFScaleOut/ScaleOut"), new ServicePartitionKey(0));
            await proxy.SendAsync(host, connectionId, $"Echo actor response :{message}").ConfigureAwait(false);
        }

        /// <summary>
        ///     This method is called whenever an actor is activated.
        ///     An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            EchoTimer = RegisterTimer(OnTimer, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            return Task.FromResult(0);
        }

        private async Task OnTimer(object arg)
        {
            await Broadcast($"Hello from Actor {Id} timer");
        }
    }
}
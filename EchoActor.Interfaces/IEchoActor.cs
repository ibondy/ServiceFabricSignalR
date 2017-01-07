namespace EchoActor.Interfaces
{
    #region Using

    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;

    #endregion

    /// <summary>
    ///     This interface defines the methods exposed by an actor.
    ///     Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IEchoActor : IActor
    {
        Task Echo(string host, string connectionId, string message);

        Task Broadcast(string message);
    }
}
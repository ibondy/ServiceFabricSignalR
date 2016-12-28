using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Collections.Generic;
using System.Fabric;

namespace SignalRHost.Internal
{
    internal class WebService:StatelessService
    {
        public WebService(StatelessServiceContext serviceContext) : base(serviceContext)
        {

        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()

        {

            return new List<ServiceInstanceListener>()

            {

                new ServiceInstanceListener(

                    (initParams) =>

                        new OwinCommunicationListener("Internal", Startup, this.Context))

            };

        }
    }
}

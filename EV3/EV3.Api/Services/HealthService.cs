using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EV3.Api.Services
{
    public class HealthServiceImp : HealthService.HealthServiceBase
    {

        public override Task<HealthResponse> CheckConnection(HealthRequest request, ServerCallContext context)
        {
            Console.WriteLine("Received HealthRequest");
            return Task.FromResult(new HealthResponse() { Healthy = true, Serverdatetime = DateTime.Now.ToString() });
        }
    }
}

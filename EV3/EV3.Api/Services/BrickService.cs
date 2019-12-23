using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EV3.Api.Services
{
    public class BrickServiceImp : BrickService.BrickServiceBase
    {
        public override async Task RegisterBricks(IAsyncStreamReader<NewBrickRequest> requestStream, IServerStreamWriter<RegisteredBrickResponse> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                Console.WriteLine("Received New Brick");
                var newBrick = requestStream.Current;
                var response = new RegisteredBrickResponse();
                response.BrickId = newBrick.BrickId;
                response.Enabled = true;
                response.AccountId = "Unknown";
                await responseStream.WriteAsync(response);
            }
        }
    }
}

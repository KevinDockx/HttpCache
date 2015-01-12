using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Marvin.HttpCache.Tests.Mock
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {

        public HttpResponseMessage Response { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
            System.Threading.CancellationToken cancellationToken)
        {

            var responseTask = new TaskCompletionSource<HttpResponseMessage>();
            responseTask.SetResult(Response);

            return responseTask.Task;
      
        }

    }
}

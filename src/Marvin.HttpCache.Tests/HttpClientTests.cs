using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Marvin.HttpCache.Tests
{
    public class HttpClientTests
    {

        public void InitClient()
        {
            var httpClient = new HttpClient(
                new HttpCacheHandler() 
                  { InnerHandler = new HttpClientHandler() 
                    });
        }
    }
}

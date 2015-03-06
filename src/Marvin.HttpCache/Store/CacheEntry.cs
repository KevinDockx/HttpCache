using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Marvin.HttpCache.Store
{
    public class CacheEntry
    {
        public CacheEntry(HttpResponseMessage httpResponse)
        {
            // TODO: Complete member initialization
            HttpResponse = httpResponse;
        }
        public HttpResponseMessage HttpResponse { get; set; }

        /// <summary>
        /// The Vary header tells any HTTP cache which parts of the request header, 
        /// other than the path and the Host header, to take into account when trying to find the right object.
        ///
        /// 
        ///
        /// The "Vary" header field in a response describes what parts of a
        /// request message, aside from the method, Host header field, and
        /// request target, might influence the origin server's process for
        /// selecting and representing this response.  The value consists of
        /// either a single asterisk ("*") or a list of header field names
        /// (case-insensitive).
        /// 
        ///   Vary = "*" / 1#field-name
        /// 
        /// A Vary field value of "*" signals that anything about the request
        /// might play a role in selecting the response representation, possibly
        /// including elements outside the message syntax (e.g., the client's
        /// network address).  A recipient will not be able to determine whether
        /// this response is appropriate for a later request without forwarding
        /// the request to the origin server.  A proxy MUST NOT generate a Vary
        /// field with a "*" value.
        /// 
        /// A Vary field value consisting of a comma-separated list of names
        /// indicates that the named request header fields, known as the
        /// selecting header fields, might have a role in selecting the
        /// representation.  The potential selecting header fields are not
        /// limited to those defined by this specification.
        /// 
        /// For example, a response that contains
        /// 
        ///   Vary: accept-encoding, accept-language
        /// 
        /// indicates that the origin server might have used the request's
        /// Accept-Encoding and Accept-Language fields (or lack thereof) as
        /// determining factors while choosing the content for this response.
        /// </summary>
        public HttpHeaderValueCollection<string> VaryHeaderCollection { get; private set; }
         
    }
}

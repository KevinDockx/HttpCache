using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marvin.HttpCache
{
    public static class HttpHeaderConstants
    {
        public const string IfNoneMatch = "If-None-Match";
        public const string IfMatch = "If-Match"; 
        public const string IfModifiedSince = "If-Modified-Since";
        public const string IfUnmodifiedSince = "If-Unmodified-Since";
        public const string LastModified = "Last-Modified";
    }          
}

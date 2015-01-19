using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marvin.HttpCache
{
    public class HttpCacheHandlerSettings : Marvin.HttpCache.IHttpCacheHandlerSettings
    {
        /// <summary>
        /// When true, IfMatch and IfUnmodifiedSince headers will be added to PUT requests if the related response
        /// was previously cached.  Default = true;
        /// </summary>
        public bool EnableConditionalPut { get; private set; }

        /// <summary>
        /// When true, IfMatch and IfUnmodifiedSince headers will be added to PATCH requests if the related response
        /// was previously cached.  Default = true;
        /// </summary>
        public bool EnableConditionalPatch { get; private set; }

        /// <summary>
        /// After a succesful PUT request, resource representations you have in your cache that were related to this request 
        /// (eg: same resource, but different represenation) might not be up to date.  Setting this to true automatically 
        /// clears all those from the cache store.  Note that this is included convenience - it overrides any header values 
        /// that might have been returned from the cache-enabled API and effectively clears all resource representations 
        /// related to the resource that was updated from the client-side cache store.  As this behaviour is often desired,
        /// default = true.  
        /// 
        /// Have a look at the project Wiki on GitHub for an example of what this entails.
        /// </summary>
        public bool EnableClearRelatedResourceRepresentationsAfterPut { get; private set; }

        /// <summary>
        /// After a succesful PATCH request, resource representations you have in your cache that were related to this request 
        /// (eg: same resource, but different represenation) might not be up to date.  Setting this to true automatically 
        /// clears all those from the cache store.  Note that this is included convenience - it overrides any header values 
        /// that might have been returned from the cache-enabled API and effectively clears all resource representations 
        /// related to the resource that was updated from the client-side cache store.  As this behaviour is often desired,
        /// default = true. 
        /// 
        /// Have a look at the project Wiki on GitHub for an example of what this entails.
        /// </summary>
        public bool EnableClearRelatedResourceRepresentationsAfterPatch { get; private set; }

        /// <summary>
        /// When true, stale resources represenations will always be validated, even if must-revalidate is false.  
        /// When false, regular cache revalidation rules will be taken into account.
        /// </summary>
        public bool ForceRevalidationOfStaleResourceRepresentations { get; private set; }

        public HttpCacheHandlerSettings(
            bool forceRevalidationOfStaleResourceRepresentations = false,
            bool enableConditionalPut = true, 
            bool enableConditionalPatch = true,
            bool enableClearRelatedResourceRepresentationsAfterPut = true,
            bool enableClearRelatedResourceRepresentationsAfterPatch = true)
        {
            ForceRevalidationOfStaleResourceRepresentations = forceRevalidationOfStaleResourceRepresentations;
            EnableConditionalPut = enableConditionalPut;
            EnableConditionalPatch = enableConditionalPatch;
            EnableClearRelatedResourceRepresentationsAfterPut = enableClearRelatedResourceRepresentationsAfterPut;
            EnableClearRelatedResourceRepresentationsAfterPatch = enableClearRelatedResourceRepresentationsAfterPatch;
        }
    }
}

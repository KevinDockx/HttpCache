using System;
namespace Marvin.HttpCache
{
    public interface IHttpCacheHandlerSettings
    {
        /// <summary>
        /// When true, IfMatch and IfUnmodifiedSince headers will be added to PUT requests if the related response
        /// was previously cached.  
        /// </summary> 
        bool EnableConditionalPatch { get; }


        /// <summary>
        /// When true, IfMatch and IfUnmodifiedSince headers will be added to PATCH requests if the related response
        /// was previously cached. 
        /// </summary>
        bool EnableConditionalPut { get; }

        /// <summary>
        /// After a succesful PUT request, resource representations you have in your cache that were related to this request 
        /// (eg: same resource, but different represenation) might not be up to date.  Setting this to true automatically 
        /// clears all those from the cache store.  Note that this is included convenience - it overrides any header values 
        /// that might have been returned from the cache-enabled API and effectively clears all resource representations 
        /// related to the resource that was updated from the client-side cache store.  
        /// 
        /// Have a look at the project Wiki on GitHub for an example of what this entails.
        /// </summary>
        bool EnableClearRelatedResourceRepresentationsAfterPatch { get; }

        /// <summary>
        /// After a succesful PATCH request, resource representations you have in your cache that were related to this request 
        /// (eg: same resource, but different represenation) might not be up to date.  Setting this to true automatically 
        /// clears all those from the cache store.  Note that this is included convenience - it overrides any header values 
        /// that might have been returned from the cache-enabled API and effectively clears all resource representations 
        /// related to the resource that was updated from the client-side cache store. 
        /// 
        /// Have a look at the project Wiki on GitHub for an example of what this entails.
        /// </summary>
        bool EnableClearRelatedResourceRepresentationsAfterPut { get; }

        /// <summary>
        /// When true, stale resource representations will always be validated, even if must-revalidate is false.  
        /// When false, regular cache revalidation rules will be taken into account.
        /// </summary>
        bool ForceRevalidationOfStaleResourceRepresentations { get; }
    }
}

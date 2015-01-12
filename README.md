# Marvin.HttpCache Introduction and Getting Started

## What Is Http Caching, and Why Do I Need It? ##

Http Caching is sometimes referred to as "the holy grail of caching".  It is fully described at [http://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html](http://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html)

It's typically used in REST-based architectural systems, and can lead to great performance improvements.  The standard consists of a server-side component (generating ETags, checking requests, …) and a client-side component (client-side cache store, composing requests, fetch from cache or from API, …).  

A great example of such a client is your web browser.  But when you're interacting with a RESTful API from .NET, you typically use an HttpClient to interact with your API, and that one does not support this standard by default.  Enter Marvin.HttpCache.

## What's Marvin.HttpCache? ##

Marvin.HttpCache is a complete implementation of the RFC2616 Http Caching standard for use with HttpClient, for all .NET Framework platforms (Windows Store, Windows Phone, WPF, WinForms, Console apps).  It handles all the hard work for you, and includes an immutable in-memory cache store to hold the cached HttpResponseMessages.  

Note that your API itself will also need to support this standard.  Marvin.HttpCache is a client-side component, not a server-side component.   If you're developing the API yourself with ASP .NET Web API, I'd suggest having a look at CacheCow.Server (NuGet: [https://www.nuget.org/packages/CacheCow.Server/](https://www.nuget.org/packages/CacheCow.Server/)).  This is a server-side implementation of this standard, for use with Web API.  

That said, Marvin.HttpCache doesn't care about how the API is built, nor in what language.  Therefore, it works with other implementations as well, and across languages.  Eg: a third-party API developed with Node.js or in Java will work as well - as long as it supports the standard, you're good to go.

## Doesn't CacheCow Have a Client-side Component as Well? ##

Yes it does, and I'm a big fan of it - but currently, that component can only be used on full .NET Framework-type applications (desktop, console).  Marvin.HttpCache was built as a small, extensible portable class library component, thus it can be used across .NET Framework platforms, ie: Windows Phone, Windows Store, WPF, WinForms, Console apps.  

## Doesn't HttpClient Cache on Its Own? ##

If you've used HttpClient in a Windows Phone app, you might've noticed it seems to cache responses on its own, without any additional configuration.  But that implementation isn't a complete nor correct implementation of the Http Caching standard and tends to lead to having to resort to ugly workarounds to get it to work as you want it to (eg: refreshing data).  I'd *really* advise you not to use it like that. 

## How Do I Get Started? ##

First, install the NuGet package (coming when ready for release).

Then, in the location you instantiate your HttpClient, instantiate it by passing in an instance of HttpCacheHandler from the Marvin.HttpCache package:

```csharp
public static class HttpClientFactory
{
    static HttpClient client;

    internal static HttpClient GetClient()
    {
        if (client == null)
        {
            client = new HttpClient
                (new HttpCacheHandler()
                      {
                          InnerHandler = new HttpClientHandler()
                      });
        }

        return client;
    }
}
```

Mind the "static".  HttpClient is intended to be used as a long-lived instance in client-side applications - re-instantiating your HttpClient by passing in a new HttpCachingHandler instance will also reset your cache store.  If you're using only one instance of HttpClient (/HttpCacheHandler), you're good to go.    

If you need to re-instantiate your HttpClient for some reason and still keep your cache store instance, that's possible as well: just ensure you pass in the cache store instance when creating the HttpCachingHandler instance.  In this example, a call to GetClient will always return a new instance, but the ICacheStore implementation will be reused:

```csharp
public static class HttpClientFactory
{
   
    static ICacheStore<string, HttpResponseMessage> _store
        = new ImmutableInMemoryCacheStore<string, HttpResponseMessage>();

    
    internal HttpClient GetClient()
    {
         return new HttpClient
                (new HttpCacheHandler(_store)
                      {
                          InnerHandler = new HttpClientHandler()
                      });          
    }
}
```


The default cache store was created as a thread-safe, immutable store with this scenario in mind - it's safe to use across different HttpClient instances & across different threads.


And that's it - you're ready to go! :)

Currently, the only cache store included is an immutable in-memory implementation (and that's the default store).  If you need other types of stores, for example: a persistent one that uses SQLite, you can implement that through the ICacheStore interface - HttpCacheHandler accepts any store in its constructor that implements that interface.  Which brings me to …

## Contributing! ##

I'm very happy to accept contributions to this codebase.  For example, if you have an ICacheStore implementation you wish to share, simply create a pull request.  The community will be grateful! :-)


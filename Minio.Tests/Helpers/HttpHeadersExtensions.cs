using System.Net.Http.Headers;
using System.Reflection;

namespace Minio.Tests.Helpers;

internal static class HttpHeadersExtensions
{
    private static readonly Action<HttpHeaders, string, string> SetRawHeaderFunc;

    static HttpHeadersExtensions()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var headerDescriptorType = assembly.GetType("System.Net.Http.Headers.HeaderDescriptor");
            if (headerDescriptorType != null)
            {
                var constructor = headerDescriptorType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(string), typeof(bool)]);
                if (constructor == null)
                    throw new InvalidOperationException("Cannot find required constructor");
                
                var httpHeader = assembly.GetType("System.Net.Http.Headers.HttpHeaders");
                if (httpHeader == null)
                    throw new InvalidOperationException("Cannot find HttpHeaders type");
                var addMember = httpHeader.GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic, [headerDescriptorType, typeof(string)]);
                if (addMember == null)
                    throw new InvalidOperationException("Cannot find internal HttpHeaders.Add(...) method");

                SetRawHeaderFunc = (httpHeaders, name, value) =>
                {
                    var hd = constructor.Invoke([name, true]);
                    addMember.Invoke(httpHeaders, [hd, value]);
                };
                return;
            }
        }

        throw new InvalidOperationException("Cannot find required types");
    }
    
    // .NET doesn't allow to set a raw header, but we need it for testing
    public static void SetRawHeader(this HttpHeaders headers, string name, string value)
    {
        SetRawHeaderFunc(headers, name, value);
    }
}
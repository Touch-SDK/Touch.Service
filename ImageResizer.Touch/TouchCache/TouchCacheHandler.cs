using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using ImageResizer.Caching;

namespace ImageResizer.Plugins.TouchCache
{
    public sealed class TouchCacheHandler : IHttpHandler
    {
        public TouchCacheHandler(IResponseArgs e, Func<Stream> factory)
        {
            _e = e;
            _factory = factory;
        }

        private readonly IResponseArgs _e;
        private readonly Func<Stream> _factory;
        private const int BufferSize = 4096;

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            var etag = _e.ResponseHeaders.Headers.AllKeys.Contains("ETag")
                ? _e.ResponseHeaders.Headers["ETag"]
                : null;
            var lastModified = _e.ResponseHeaders.LastModified;

            _e.ResponseHeaders.ApplyDuringPreSendRequestHeaders = false;
            _e.ResponseHeaders.ApplyToResponse(_e.ResponseHeaders, context);

            context.Response.BufferOutput = false;

            var headers = context.Request.Headers;

            DateTime ifModifiedSince;
            if (headers.AllKeys.Contains("If-Modified-Since") && DateTime.TryParse(headers["If-Modified-Since"], out ifModifiedSince) && lastModified <= ifModifiedSince)
            {
                context.Response.StatusCode = 304;
                context.Response.End();
                return;
            }

            if (headers.AllKeys.Contains("If-None-Match") && headers["If-None-Match"] == etag)
            {
                context.Response.StatusCode = 304;
                context.Response.End();
                return;
            }

            context.Response.StatusCode = 200;

            var buffer = new byte[BufferSize];

            using (var data = _factory())
            {
                int i;
                while ((i = data.Read(buffer, 0, buffer.Length)) > 0)
                {
                    context.Response.OutputStream.Write(buffer, 0, i);
                }

                context.Response.End();
            }
        }
    }
}

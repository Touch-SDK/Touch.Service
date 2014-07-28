using System.IO;
using System.Web;
using ImageResizer.Caching;

namespace ImageResizer.Plugins.TouchCache
{
    public sealed class TouchCacheHandler : IHttpHandler
    {
        public TouchCacheHandler(IResponseArgs e, Stream data)
        {
            _e = e;
            _data = data;
        }

        private readonly IResponseArgs _e;
        private readonly Stream _data;
        private const int BufferSize = 4096;

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.StatusCode = 200;
            context.Response.BufferOutput = false;

            _e.ResponseHeaders.ApplyDuringPreSendRequestHeaders = false;
            _e.ResponseHeaders.ApplyToResponse(_e.ResponseHeaders, context);

            var buffer = new byte[BufferSize];

            using (_data)
            {
                int i;
                while ((i = _data.Read(buffer, 0, buffer.Length)) > 0)
                {
                    context.Response.OutputStream.Write(buffer, 0, i);
                }
            }
        }
    }
}

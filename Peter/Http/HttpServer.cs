using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peter.Http
{
    public class HttpServer
    {
        private readonly HttpListener httpListener = new HttpListener();
        private readonly ConcurrentBag<Task> pending = new ConcurrentBag<Task>();

        public async Task ListenAsync(string host, HttpRouter router, CancellationToken token)
        {
            httpListener.Prefixes.Clear();
            httpListener.Prefixes.Add(host);
            httpListener.Start();
            using (token.Register(httpListener.Stop))
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var ctx = await httpListener.GetContextAsync().ConfigureAwait(false);
                        var t = router.ServeHttp(ctx.Request, ctx.Response, token);
                        pending.Add(t);
                        _ = t.ContinueWith(x => pending.TryTake(out _));
                    }
                    catch (HttpListenerException hlx) when (hlx.ErrorCode == 995)
                    { /* swallow cancellations */ }
                }
            }
        }

        public void Stop() => httpListener.Stop();
        public void Abort() => httpListener.Abort();
    }

    public class HttpContext
    {
        public HttpListenerRequest Request { get; }
        public HttpListenerResponse Response { get; }
        public CancellationToken Cancellation { get; }
        public string HandlerPath { get; }

        public HttpContext(HttpListenerRequest request, HttpListenerResponse response, string handlerPath, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(handlerPath))
            {
                throw new ArgumentException("message", nameof(handlerPath));
            }

            Request = request ?? throw new ArgumentNullException(nameof(request));
            Response = response ?? throw new ArgumentNullException(nameof(response));
            HandlerPath = handlerPath;
            Cancellation = cancellation;
        }

        public Task WriteStringAsync(string value, HttpStatusCode statusCode = HttpStatusCode.OK, CancellationToken cancellationToken = default)
        {
            return WriteStringAsync(value, Encoding.UTF8, statusCode, cancellationToken);
        }

        public Task WriteStringAsync(string value, Encoding encoding, HttpStatusCode statusCode = HttpStatusCode.OK, CancellationToken cancellationToken = default)
        {
            var data = encoding.GetBytes(value);
            Response.StatusCode = (int)statusCode;
            return Response.OutputStream.WriteAsync(data, 0, data.Length, cancellationToken);
        }

        public Task WriteAsync(byte[] value, HttpStatusCode statusCode = HttpStatusCode.OK, CancellationToken cancellationToken = default)
        {
            Response.StatusCode = (int)statusCode;
            return Response.OutputStream.WriteAsync(value, 0, value.Length, cancellationToken);
        }

        public Task WriteStreamAsync(Stream stream, HttpStatusCode statusCode = HttpStatusCode.OK, CancellationToken cancellationToken = default)
        {
            Response.StatusCode = (int)statusCode;
            return stream.CopyToAsync(Response.OutputStream, 81920, cancellationToken);
        }
    }

    public class HttpRouter
    {
        private readonly List<HttpHandler> handlers = new List<HttpHandler>();

        public void HandleRequest(string path, Func<HttpContext, Task> handleFunc)
        {
            handlers.Add(new HttpHandler(path, handleFunc));
        }

        public async Task ServeHttp(HttpListenerRequest r, HttpListenerResponse re, CancellationToken token)
        {
            var path = r.Url.AbsolutePath;
            HttpHandler handler = GetHandlerForPath(path);
            if (!(handler is null))
            {
                try
                {
                    await handler.HandleAsync(new HttpContext(r, re, handler.Path, token)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error handling request '{0}'", ex.Message);
                }
            }
            else
            {
                Console.WriteLine("No Handler for route '{0}'", r.Url.AbsolutePath);
            }

            r.InputStream.Dispose();
            re.OutputStream.Dispose();
        }

        private HttpHandler GetHandlerForPath(string path)
        {
            for (int i = 0; i < handlers.Count; i++)
            {
                var currentHandler = handlers[i];
                if (path.StartsWith(currentHandler.Path, StringComparison.OrdinalIgnoreCase))
                {
                    return currentHandler;
                }
            }
            return null;
        }

        private class HttpHandler
        {
            private readonly Func<HttpContext, Task> handleFunc;

            public string Path { get; }

            public HttpHandler(string path, Func<HttpContext, Task> handleFunc)
            {
                Path = !string.IsNullOrEmpty(path) ? path : throw new ArgumentException("value can not be null or empty.", nameof(path));
                this.handleFunc = handleFunc ?? throw new ArgumentNullException(nameof(handleFunc));
            }

            public Task HandleAsync(HttpContext ctx)
            {
                if (ctx == null) throw new ArgumentNullException(nameof(ctx));
                return handleFunc(ctx);
            }
        }

        public static Func<HttpContext, Task> FileServer(string root)
        {
            return async (HttpContext ctx) =>
            {
                string filePath;
                if (ctx.HandlerPath[ctx.HandlerPath.Length - 1] == '/')
                {
                    filePath = ctx.Request.Url.AbsolutePath.Substring(ctx.HandlerPath.Length);
                }
                else if (ctx.Request.Url.AbsolutePath.Length > ctx.HandlerPath.Length + 1)
                {
                    filePath = ctx.Request.Url.AbsolutePath.Substring(ctx.HandlerPath.Length + 1);
                }
                else
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                if (string.IsNullOrEmpty(filePath) || filePath.Contains('\\') || filePath.Contains(':') || filePath[0] == '/')
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                filePath = Path.Combine(root, filePath);
                if (!File.Exists(filePath))
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                using (var fs = File.OpenRead(filePath))
                {
                    ctx.Response.ContentType = "application/octet-stream";
                    await ctx.WriteStreamAsync(fs, cancellationToken: ctx.Cancellation);
                }
            };
        }
    }
}

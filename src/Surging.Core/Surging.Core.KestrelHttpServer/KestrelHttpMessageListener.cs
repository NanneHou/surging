﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Surging.Core.CPlatform.Messages;
using Surging.Core.CPlatform.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Surging.Core.KestrelHttpServer.Builder;
using Microsoft.AspNetCore.Http;
using Surging.Core.CPlatform.Serialization;

namespace Surging.Core.KestrelHttpServer
{
    public class KestrelHttpMessageListener : HttpMessageListener, IDisposable
    {
        private readonly ILogger<KestrelHttpMessageListener> _logger;
        private IWebHost _host;
        private readonly ISerializer<string> _serializer;
         

        public KestrelHttpMessageListener(ILogger<KestrelHttpMessageListener> logger, ISerializer<string> serializer) :base(logger, serializer)
        {
            _logger = logger;
            _serializer = serializer;
        }
        
        public async Task StartAsync(EndPoint endPoint)
        {
            var ipEndPoint = endPoint as IPEndPoint; 
            try
            {
                _host = new WebHostBuilder()
                 .UseContentRoot(Directory.GetCurrentDirectory())
                 .UseKestrel(options=> {
                     options.Listen(ipEndPoint);

                 })
                 .ConfigureLogging((logger) => {
                     logger.AddConfiguration(
                            CPlatform.AppConfig.GetSection("Logging"));
                 })
                 .Configure(AppResolve)
                 .Build();

               await _host.RunAsync();
            }
            catch
            {
                _logger.LogError($"http服务主机启动失败，监听地址：{endPoint}。 ");
            }

        }
        

        private void AppResolve(IApplicationBuilder app)
        { 
            app.Run(async (context) =>
            {
                var sender =new HttpServerMessageSender(_serializer,context);
                await OnReceived(sender,context);
            });
        }

        public void Dispose()
        {
            _host.Dispose();
        }
        
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Configuration;
using BTCPayServer.Tor;

namespace BTCPayServer.Services
{
    public class SocketFactory
    {
        private readonly BTCPayServerOptions _options;
        public SocketFactory(BTCPayServerOptions options)
        {
            _options = options;
        }
        public async Task<Socket> ConnectAsync(EndPoint endPoint, SocketType socketType, ProtocolType protocolType, CancellationToken cancellationToken)
        {
            Socket socket = null;
            try
            {
                if (endPoint is IPEndPoint ipEndpoint)
                {
                    socket = new Socket(ipEndpoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(ipEndpoint).WithCancellation(cancellationToken);
                }
                else if (endPoint is OnionEndpoint onionEndpoint)
                {
                    if (_options.SocksEndpoint == null)
                        throw new NotSupportedException("It is impossible to connect to an onion address without btcpay's -socksendpoint configured");
                    socket = await Socks5Connect.ConnectSocksAsync(_options.SocksEndpoint, onionEndpoint, cancellationToken);
                }
                else if (endPoint is DnsEndPoint dnsEndPoint)
                {
                    var address = (await Dns.GetHostAddressesAsync(dnsEndPoint.Host)).FirstOrDefault();
                    socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(dnsEndPoint).WithCancellation(cancellationToken);
                }
                else
                    throw new NotSupportedException("Endpoint type not supported");
            }
            catch
            {
                CloseSocket(ref socket);
                throw;
            }
            return socket;
        }

        private void CloseSocket(ref Socket s)
        {
            if (s == null)
                return;
            try
            {
                s.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                try
                {
                    s.Dispose();
                }
                catch { }
            }
            finally
            {
                s = null;
            }
        }
    }
}

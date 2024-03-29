﻿using System.Net;
using System.Net.Sockets;

namespace AspNetCore.SpaServices.ViteDevelopmentServer.Util;

internal static class TcpPortFinder
{
    public static int FindAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
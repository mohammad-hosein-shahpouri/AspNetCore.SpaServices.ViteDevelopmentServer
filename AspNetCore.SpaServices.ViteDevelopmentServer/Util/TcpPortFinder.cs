using System.Net;
using System.Net.Sockets;

namespace AspNetCore.SpaServices.ViteDevelopmentServer.Util;

internal static class TcpPortFinder
{
    /// <summary>
    /// Will return an available TCP port on the local machine.
    /// </summary>
    /// <param name="disallowedPort">When supplied, this port will be disallowed</param>
    /// <returns></returns>
    public static int FindAvailablePort(int? disallowedPort)
    {
        int port;
        TcpListener? listener = null;

        do
        {
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            port = ((IPEndPoint)listener.LocalEndpoint).Port;

            if (disallowedPort == port)
            {
                listener.Stop();
                listener = null;
            }
        } while (listener == null);

        try
        {
            return port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
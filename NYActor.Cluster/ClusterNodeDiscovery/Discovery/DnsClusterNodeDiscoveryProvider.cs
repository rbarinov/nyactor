using System.Net;
using System.Net.Sockets;

namespace NYActor.Cluster.ClusterNodeDiscovery.Discovery;

public class DnsClusterNodeDiscoveryProvider : IClusterNodeDiscoveryProvider
{
    private readonly string _hostname;
    private readonly int _port;

    public DnsClusterNodeDiscoveryProvider(
        TimeSpan discoveryInterval,
        string hostname,
        int port
    )
    {
        _hostname = hostname;
        _port = port;
        DiscoveryInterval = discoveryInterval;
    }

    public TimeSpan DiscoveryInterval { get; }

    public async Task<IReadOnlyCollection<ClusterNodeDiscoveryNodeInfo>> DiscoverAsync()
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(_hostname);

            var nodes = addresses.Select(
                    e => e.AddressFamily switch
                    {
                        AddressFamily.InterNetwork => e.MapToIPv4()
                            .ToString(),
                        AddressFamily.InterNetworkV6 => e.MapToIPv6()
                            .ToString(),
                        _ => null
                    }
                )
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(
                    e => new ClusterNodeDiscoveryNodeInfo(
                        e,
                        _port
                    )
                )
                .ToList()
                .AsReadOnly();

            return nodes;
        }
        catch (SocketException)
        {
            return null;
        }
    }
}

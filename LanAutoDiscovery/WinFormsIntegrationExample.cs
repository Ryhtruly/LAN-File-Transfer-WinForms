using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LanAutoDiscovery;

public sealed class WinFormsIntegrationExample
{
    private readonly ComboBox _serverDropdown;
    private readonly LanClientDiscoveryService _discoveryService;

    public WinFormsIntegrationExample(ComboBox serverDropdown)
    {
        _serverDropdown = serverDropdown;
        _discoveryService = new LanClientDiscoveryService();
        _discoveryService.ServersChanged += OnServersChanged;
    }

    public void StartDiscovery()
    {
        _discoveryService.Start();
    }

    public async Task StopDiscoveryAsync()
    {
        _discoveryService.ServersChanged -= OnServersChanged;
        await _discoveryService.StopAsync().ConfigureAwait(false);
    }

    public DiscoveredServer? GetSelectedServer()
    {
        return _serverDropdown.SelectedItem as DiscoveredServer;
    }

    private void OnServersChanged(object? sender, IReadOnlyList<DiscoveredServer> servers)
    {
        if (_serverDropdown.InvokeRequired)
        {
            _serverDropdown.BeginInvoke(new Action(() => BindServers(servers)));
            return;
        }

        BindServers(servers);
    }

    private void BindServers(IReadOnlyList<DiscoveredServer> servers)
    {
        var selectedServerId = (_serverDropdown.SelectedItem as DiscoveredServer)?.ServerId;

        _serverDropdown.BeginUpdate();
        _serverDropdown.DataSource = servers.ToList();
        _serverDropdown.DisplayMember = nameof(DiscoveredServer.DisplayName);
        _serverDropdown.EndUpdate();

        if (selectedServerId is null)
        {
            return;
        }

        var selectedIndex = servers
            .Select((server, index) => new { server.ServerId, index })
            .FirstOrDefault(item => item.ServerId == selectedServerId)?.index ?? -1;

        _serverDropdown.SelectedIndex = selectedIndex;
    }
}

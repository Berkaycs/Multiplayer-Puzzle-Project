using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private string _joinCode;
    private string _ip;
    private string _port;
    private byte[] _key;
    private byte[] _connectionData;
    private byte[] _hostConnectionData;
    private Guid _allocationId;
    private byte[] _allocationIdBytes;
    private bool _isHost;

    public bool IsHost
    {
        get { return _isHost; }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            DontDestroyOnLoad(Instance);
        }
    }

    public async Task<string> CreateRelay(int maxConnections)
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        _joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        RelayServerEndpoint dtlsEndpoint = allocation.ServerEndpoints.First(endpoint => endpoint.ConnectionType == "dtls");
        _ip = dtlsEndpoint.Host;
        _port = dtlsEndpoint.Port.ToString();

        _allocationId = allocation.AllocationId;
        _allocationIdBytes = allocation.AllocationIdBytes;
        _connectionData = allocation.ConnectionData;
        _key = allocation.Key;

        _isHost = true;

        return _joinCode;
    }

    public async Task<bool> JoinRelay(string joinCode)
    {
        _joinCode = joinCode;
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        RelayServerEndpoint dtlsEndpoint = allocation.ServerEndpoints.First(endpoint => endpoint.ConnectionType == "dtls");
        _ip = dtlsEndpoint.Host;
        _port = dtlsEndpoint.Port.ToString();

        _allocationId = allocation.AllocationId;
        _allocationIdBytes = allocation.AllocationIdBytes;
        _connectionData = allocation.ConnectionData;
        _hostConnectionData = allocation.HostConnectionData;
        _key = allocation.Key;

        return true;
    }

    public (byte[] AllocationId, byte[] Key, byte[] ConnectionData, string _dtlsAdress, int _dtlsPort) GetHostConnectionInfo()
    {
        return (_allocationIdBytes, _key, _connectionData, _ip, int.Parse(_port));
    }

    public (byte[] AllocationId, byte[] Key, byte[] ConnectionData, byte[] HostConnectionData, string _dtlsAdress, int _dtlsPort) GetClientConnectionInfo()
    {
        return (_allocationIdBytes, _key, _connectionData, _hostConnectionData, _ip, int.Parse(_port));
    }

    public string GetAllocationId()
    {
        return _allocationId.ToString();
    }

    public string GetConnectionData()
    {
        return _connectionData.ToString();
    }
}

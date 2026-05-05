using System;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Networking.Transport.Relay;

namespace CoopPuzzle.Lobby
{
    public sealed class RelayServiceFacade
    {
        public async Task<(string joinCode, RelayServerData serverData)> CreateRelayAsync(int maxConnections)
        {
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                var serverData = new RelayServerData(allocation, "dtls");
                return (joinCode, serverData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Relay CreateAllocation hata: {ex}");
                throw;
            }
        }

        public async Task<RelayServerData> JoinRelayAsync(string joinCode)
        {
            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                return new RelayServerData(joinAllocation, "dtls");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Relay JoinAllocation hata: {ex}");
                throw;
            }
        }
    }
}


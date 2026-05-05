using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using Unity.Networking.Transport.Relay;

namespace CoopPuzzle.Core.Bootstrap
{
    public sealed class NetworkBootstrap : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport unityTransport;

        private void Reset()
        {
            networkManager = FindFirstObjectByType<NetworkManager>();
            unityTransport = FindFirstObjectByType<UnityTransport>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (networkManager == null)
                networkManager = FindFirstObjectByType<NetworkManager>();

            if (unityTransport == null)
                unityTransport = FindFirstObjectByType<UnityTransport>();
        }

        public void ConfigureRelay(RelayServerData relayServerData)
        {
            if (unityTransport == null)
            {
                Debug.LogError("UnityTransport bulunamadı. NetworkBootstrap sahnesinde UnityTransport olmalı.");
                return;
            }

            unityTransport.SetRelayServerData(relayServerData);
        }

        public void StartHost()
        {
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager bulunamadı. NetworkBootstrap sahnesinde NetworkManager olmalı.");
                return;
            }

            networkManager.StartHost();
        }

        public void StartClient()
        {
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager bulunamadı. NetworkBootstrap sahnesinde NetworkManager olmalı.");
                return;
            }

            networkManager.StartClient();
        }
    }
}


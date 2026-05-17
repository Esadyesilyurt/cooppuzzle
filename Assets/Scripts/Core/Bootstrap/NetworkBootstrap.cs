using System;
using System.Reflection;
using CoopPuzzle.Gameplay.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Networking.Transport.Relay;

namespace CoopPuzzle.Core.Bootstrap
{
    [DefaultExecutionOrder(-200)]
    public sealed class NetworkBootstrap : MonoBehaviour
    {
        private static readonly PropertyInfo SceneManagerProperty =
            typeof(NetworkManager).GetProperty(nameof(NetworkManager.SceneManager));

        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport unityTransport;

        private void Reset()
        {
            networkManager = FindFirstObjectByType<NetworkManager>();
            unityTransport = FindFirstObjectByType<UnityTransport>();
        }

        private void Awake()
        {
            ResolveNetworkRefs();
            EnsureTransportWired();

            if (networkManager != null)
                DontDestroyOnLoad(networkManager.gameObject);
        }

        private void OnApplicationQuit()
        {
            ResolveNetworkRefs();

            if (networkManager == null)
                return;

            var active = networkManager.IsListening || networkManager.IsHost ||
                         networkManager.IsClient || networkManager.IsServer;

            if (active)
            {
                try
                {
                    networkManager.Shutdown();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Network] Shutdown: {ex.Message}");
                }
            }
            else
            {
                DetachSceneManagerIfIdle();
            }
        }

        private void ResolveNetworkRefs()
        {
            if (networkManager == null)
                networkManager = FindAnyObjectByType<NetworkManager>();

            if (unityTransport == null && networkManager != null)
                unityTransport = networkManager.GetComponent<UnityTransport>();

            if (unityTransport == null)
                unityTransport = FindAnyObjectByType<UnityTransport>();
        }

        private void DetachSceneManagerIfIdle()
        {
            if (networkManager == null)
                return;

            if (networkManager.IsListening || networkManager.IsHost ||
                networkManager.IsClient || networkManager.IsServer)
                return;

            if (SceneManagerProperty == null)
                return;

            if (SceneManagerProperty.GetValue(networkManager) == null)
                return;

            SceneManagerProperty.SetValue(networkManager, null);
        }

        private void EnsureTransportWired()
        {
            if (networkManager == null)
                return;

            if (unityTransport == null)
                unityTransport = networkManager.GetComponent<UnityTransport>();

            if (unityTransport == null)
                unityTransport = FindAnyObjectByType<UnityTransport>();

            if (unityTransport == null)
            {
                Debug.LogError("UnityTransport bulunamadı. NetworkManager ile aynı GameObject'te UnityTransport olmalı.");
                return;
            }

            if (networkManager.NetworkConfig.NetworkTransport != unityTransport)
                networkManager.NetworkConfig.NetworkTransport = unityTransport;
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
            ResolveNetworkRefs();

            if (networkManager == null)
            {
                Debug.LogError("NetworkManager bulunamadı. NetworkBootstrap sahnesinde NetworkManager olmalı.");
                return;
            }

            EnsureTransportWired();

            if (networkManager.NetworkConfig.NetworkTransport == null)
            {
                Debug.LogError("Network transport atanmadı. Tools > CoopPuzzle > Setup > Setup Lobby Scene çalıştır.");
                return;
            }

            try
            {
                if (!networkManager.StartHost())
                {
                    Debug.LogError("[Network] StartHost başarısız. Relay/transport ayarlarını kontrol et.");
                    DetachSceneManagerIfIdle();
                }
            }
            catch (Exception ex)
            {
                DetachSceneManagerIfIdle();
                Debug.LogException(ex);
                throw;
            }
        }

        public void StartClient()
        {
            ResolveNetworkRefs();

            if (networkManager == null)
            {
                Debug.LogError("NetworkManager bulunamadı. NetworkBootstrap sahnesinde NetworkManager olmalı.");
                return;
            }

            EnsureTransportWired();

            if (networkManager.NetworkConfig.NetworkTransport == null)
            {
                Debug.LogError("Network transport atanmadı. Tools > CoopPuzzle > Setup > Setup Lobby Scene çalıştır.");
                return;
            }

            try
            {
                if (!networkManager.StartClient())
                    Debug.LogError("[Network] StartClient başarısız.");
            }
            catch (Exception ex)
            {
                DetachSceneManagerIfIdle();
                Debug.LogException(ex);
                throw;
            }
        }

        public bool IsHostOrServer =>
            networkManager != null && (networkManager.IsHost || networkManager.IsServer);

        public bool LoadGameplayScene(string sceneName = null)
        {
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager yok.");
                return false;
            }

            if (!networkManager.IsServer)
            {
                Debug.LogWarning("Gameplay sahnesini yalnızca host yükleyebilir.");
                return false;
            }

            if (networkManager.SceneManager == null)
            {
                Debug.LogError("NetworkSceneManager yok. NetworkManager'da Scene Management açık olmalı.");
                return false;
            }

            sceneName = string.IsNullOrWhiteSpace(sceneName) ? GameplayScenes.Gameplay : sceneName;
            var status = networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            var ok = status == SceneEventProgressStatus.Started;
            Debug.Log($"[Network] LoadScene '{sceneName}' → {status} (ok={ok})");
            return ok;
        }
    }
}

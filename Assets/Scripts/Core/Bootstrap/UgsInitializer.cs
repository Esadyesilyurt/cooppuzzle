using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace CoopPuzzle.Core.Bootstrap
{
    public sealed class UgsInitializer : MonoBehaviour
    {
        private const int InitTimeoutSeconds = 45;

        public static bool IsInitialized { get; private set; }

        private Task _initTask;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _ = InitializeIfNeededAsync();
        }

        public Task InitializeIfNeededAsync()
        {
            if (IsInitialized) return Task.CompletedTask;

            if (_initTask != null && !_initTask.IsFaulted && !_initTask.IsCanceled)
                return _initTask;

            _initTask = InitializeAsync();
            return _initTask;
        }

        private static async Task InitializeAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await WithTimeout(UnityServices.InitializeAsync(), InitTimeoutSeconds, "Unity Services Initialize");

                if (!AuthenticationService.Instance.IsSignedIn)
                    await WithTimeout(AuthenticationService.Instance.SignInAnonymouslyAsync(), InitTimeoutSeconds, "Anonymous Sign-In");

                IsInitialized = true;
                Debug.Log($"[UGS] Hazır. PlayerId={AuthenticationService.Instance.PlayerId}");
            }
            catch (TimeoutException)
            {
                IsInitialized = false;
                throw;
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                Debug.LogError($"[UGS] Başlatılamadı: {ex.Message}");
                Debug.LogException(ex);
                throw;
            }
        }

        private static async Task WithTimeout(Task task, int timeoutSeconds, string label)
        {
            var delay = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            var completed = await Task.WhenAny(task, delay);
            if (completed != task)
            {
                var msg = $"{label} zaman aşımı ({timeoutSeconds}s). İnternet ve Dashboard ayarlarını kontrol et.";
                Debug.LogError($"[UGS] {msg}");
                throw new TimeoutException(msg);
            }

            await task;
        }
    }
}


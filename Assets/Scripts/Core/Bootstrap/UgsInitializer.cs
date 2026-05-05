using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace CoopPuzzle.Core.Bootstrap
{
    public sealed class UgsInitializer : MonoBehaviour
    {
        public static bool IsInitialized { get; private set; }

        private Task _initTask;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _initTask = InitializeIfNeededAsync();
        }

        public Task InitializeIfNeededAsync()
        {
            if (IsInitialized) return Task.CompletedTask;

            // Prevent duplicate inits if multiple instances exist.
            if (_initTask != null) return _initTask;

            _initTask = InitializeAsync();
            return _initTask;
        }

        private static async Task InitializeAsync()
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            IsInitialized = true;
        }
    }
}


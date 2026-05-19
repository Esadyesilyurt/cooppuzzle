using Unity.Netcode;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Player
{
    public static class LocalPlayerLookup
    {
        public static NetworkTravelerController GetLocalTraveler()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return null;

            var playerObject = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (playerObject != null)
            {
                var controller = playerObject.GetComponent<NetworkTravelerController>();
                if (controller != null)
                    return controller;
            }

            foreach (var traveler in Object.FindObjectsByType<NetworkTravelerController>(FindObjectsInactive.Exclude))
            {
                if (traveler != null && traveler.IsOwner)
                    return traveler;
            }

            return null;
        }

        public static Transform GetLocalTravelerTransform() => GetLocalTraveler()?.transform;
    }
}

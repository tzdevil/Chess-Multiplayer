using Chess.GameRelated;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Chess.NetworkRelated
{
    public class ChessNetworkManager : NetworkManager
    {
        public static ChessNetworkManager Instance;

        private void Awake() => Instance = this;

        private void Start()
        {
            OnClientConnectedCallback += OnClientConnect;
        }

        private void OnClientConnect(ulong id)
        {
            Setup(id);
        }

        private void Setup(ulong id)
        {
            var whitePlayer = SpawnManager.SpawnedObjects.Where(k => k.Value.GetComponent<PlayerManager>() != null).ToArray()[0].Value.gameObject;

            whitePlayer.name = "White";
            whitePlayer.transform.SetPositionAndRotation(new(-0.240633f, -5.714224f, 7.655799f), Quaternion.Euler(0, 0, 0));

            if (id == 0)
                return;

            var blackPlayer = SpawnManager.SpawnedObjects.Where(k => k.Value.GetComponent<PlayerManager>() != null).ToArray()[1].Value.gameObject;

            blackPlayer.name = "Black";
            blackPlayer.transform.SetPositionAndRotation(new(-1.753429f, -5.714224f, 25.47488f), Quaternion.Euler(0, 180, 0));

            GameManager.Instance.RegisterChessPieces();

        }
    }
}
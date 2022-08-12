using Chess.NetworkRelated;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Chess.LobbyRelated
{
    public class LobbyManager : MonoBehaviour
    {
        [SerializeField] private GameObject _canvasBefore;
        [SerializeField] private GameObject _connectingText;

        [SerializeField] private List<GameObject> _playerList = new();

        public void HostServer()
        {
            ChessNetworkManager.Instance.StartHost();
            SetUIState();
        }

        public void ConnectClient()
        {
            ChessNetworkManager.Instance.StartClient();
            SetUIState();
        }

        private async void SetUIState()
        {
            _connectingText.SetActive(true);
            await Task.Delay(1000);
            _canvasBefore.SetActive(false);
        }
    }
}
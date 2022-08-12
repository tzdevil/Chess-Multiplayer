using Chess.GameRelated;
using Chess.PieceRelated;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Chess.NetworkRelated
{
    public class PlayerManager : NetworkBehaviour
    {
        private PieceColor _playerColor => Enum.Parse<PieceColor>(gameObject.name);

        [Header("Coding Related")]
        public NetworkVariable<bool> InCheckPosition;
        public NetworkVariable<bool> IsCheckmate;
        public bool PlayerIsHost => IsHost;

        [Header("References")]
        [SerializeField] private PieceInfo _king;
        [SerializeField] private PieceInfo _fakeKing;

        #region Syncing Data
        public override async void OnNetworkSpawn()
        {
            await Task.Delay(500);

            // Change piece color.
            foreach (Transform piece in transform)
            {
                var pieceInfo = piece.GetComponent<PieceInfo>();
                pieceInfo.PieceColor = _playerColor;
                pieceInfo.CheckPieceColor();
            }

            _fakeKing.Cell = _king.Cell;
        }
        #endregion

        #region Checkmate Scenario
        // This creates a movable area for King after Check.
        public async void CreateMovableAreaForKingAfterCheck()
        {
            await Task.Delay(100);

            _fakeKing.gameObject.SetActive(true);
            _king.GetComponent<BoxCollider>().enabled = false;
            _fakeKing.MovableArea.Clear();

            var currentMovableArea = new List<GameObject>(_king.MovableArea);
            var newMovableArea = new List<GameObject>(currentMovableArea);

            foreach (GameObject cell in currentMovableArea)
            {
                _fakeKing.MovePieceToNewCellWithoutChangingTurn(cell);

                await Task.Delay(25);

                GameManager.Instance.UpdateChessPieces();

                await Task.Delay(25);

                var opponentPieces = GameManager.Instance.ChessPieces.Where(p => p.PieceColor != _playerColor).ToList();

                foreach (var piece in opponentPieces)
                {
                    if (piece.PossiblePiecesYouCanEat.Any(p => p.name == _fakeKing.gameObject.name))
                    {
                        newMovableArea.Remove(cell);
                    }
                }
            }

            _fakeKing.transform.position = _king.transform.position;
            GameManager.Instance.UpdateChessPieces();
            _king.MovableArea = new List<GameObject>(newMovableArea);
            _fakeKing.gameObject.SetActive(false);
            _king.GetComponent<BoxCollider>().enabled = true;

            CheckmateStatus();

            _king.SetEatablePieces(new() { transform.forward, -transform.forward, transform.right, -transform.right, transform.forward + transform.right, transform.forward - transform.right, -transform.forward + transform.right, -transform.forward - transform.right });
        }

        // Display Game Over Canvas
        private void CheckmateStatus()
        {
            IsCheckmate.Value = _king.MovableArea.Count == 0;

            if (IsCheckmate.Value)
                GameManager.Instance.CheckmateClientRpc(_playerColor);
        }
        #endregion
    }
}
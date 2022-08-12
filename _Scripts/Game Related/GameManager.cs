using Chess.PieceRelated;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Chess.GameRelated
{
    public class GameManager : NetworkBehaviour
    {
        [Header("References")]
        public static GameManager Instance;
        public Camera Cam;
        [SerializeField] private GameObject _gameOverCanvas;
        [SerializeField] private TMP_Text _checkmateText;

        [Header("Materials & Sprites")]
        public Material BlackMaterial;
        public Sprite BlackPawn;
        public Sprite BlackRook;
        public Sprite BlackKnight;
        public Sprite BlackBishop;
        public Sprite BlackQueen;
        public Sprite BlackKing;

        [Header("Game Mechanics")]
        public PieceColor Turn; // networkvariable'a çevireceðim.
        public List<PieceInfo> ChessPieces;

        #region Game Related
        private void Awake() => Instance = this;

        public void RegisterChessPieces()
        {
            var pieces = GameObject.FindGameObjectsWithTag("Piece");
            ChessPieces.AddRange(pieces.Select(k => k.GetComponent<PieceInfo>()));
        }

        public void UpdateChessPieces()
        {
            foreach (PieceInfo piece in ChessPieces)
                piece.CheckRaycast();
        }

        #endregion

        #region Syncing Data
        [ServerRpc(RequireOwnership = false)]
        public void ChangeTurnServerRpc(PieceColor turn) => ChangeTurnClientRpc(turn);

        [ClientRpc]
        public void ChangeTurnClientRpc(PieceColor turn)
        {
            // Change turn.
            Turn = turn == PieceColor.White ? PieceColor.Black : PieceColor.White;

            UpdateChessPieces();
        }

        [ServerRpc(RequireOwnership = false)]
        public void EatPieceServerRpc(int id)
        {
            EatPieceClientRpc(id);
        }

        [ClientRpc]
        public void EatPieceClientRpc(int id)
        {
            var piece = ChessPieces[id];
            piece.gameObject.SetActive(false);
            GameManager.Instance.ChessPieces.Remove(piece.GetComponent<PieceInfo>());
        }

        [ClientRpc]
        public void CheckmateClientRpc(PieceColor playerColor)
        {
            _checkmateText.text = $"Checkmate!\n{(playerColor == PieceColor.White ? "Black" : "White")} wins!";
            _gameOverCanvas.SetActive(true);
        }
        #endregion
    }
}
using Chess.GameRelated;
using Chess.NetworkRelated;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Chess.PieceRelated
{
    public class PieceInfo : MonoBehaviour
    {
        [Header("Piece Information")]
        public PieceType PieceType;
        public GameObject Cell;
        public GameObject CellColliding;
        [SerializeField] private int _maxDistance = 8;
        public PieceColor PieceColor;
        private const float PIECE_Y_POSITION = -5.307906f;
        [SerializeField] private SpriteRenderer _sprite;

        [Header("Coding Related")]
        public List<GameObject> MovableArea;
        public Vector3 DistanceToMiddle;
        private bool _isHolding;
        private bool _hasMoved = false;
        public List<GameObject> PossiblePiecesYouCanEat;

        [Header("References")]
        private PlayerManager _playerManager;
        private BoxCollider _boxCollider;
        [SerializeField] private Material _grey;
        [SerializeField] private Material _black;
        [SerializeField] private Material _playable;

        void Awake() => Init();

        private async void Init()
        {
            _playerManager = transform.parent.GetComponent<PlayerManager>();
            _boxCollider = GetComponent<BoxCollider>();
            // A bit of a delay while we're waiting for OnTrigger to trigger.
            await Task.Delay(200);
            MoveToCell(true);

            // Reset pieces.
            _hasMoved = false;

            // A pawn can move 2 cells in one if it's their first move.
            if (gameObject.name == "Pawn" && !_hasMoved)
                _maxDistance = 2;

            // Check first raycast.
            CheckRaycast();
        }

        public void CheckPieceColor()
        {
            // Change piece colors if it's the Client.
            if (PieceColor == PieceColor.Black)
            {
                _sprite.sprite = PieceType switch
                {
                    PieceType.Pawn => GameManager.Instance.BlackPawn,
                    PieceType.Rook => GameManager.Instance.BlackRook,
                    PieceType.Knight => GameManager.Instance.BlackKnight,
                    PieceType.Bishop => GameManager.Instance.BlackBishop,
                    PieceType.Queen => GameManager.Instance.BlackQueen,
                    PieceType.King => GameManager.Instance.BlackKing,
                    _ => throw new System.NotImplementedException(),
                };
                _sprite.transform.rotation = Quaternion.Euler(90, 180, 0);
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (!collider.gameObject.CompareTag("Cell"))
                return;

            // I use CellColliding after OnPieceUp to determine the block player wanted to play. I don't know if it's more efficient, but it's more useful than Raycast.
            CellColliding = collider.gameObject;

            if (_isHolding)
                return;

            // This triggers at start to calculate piece's location.
            Cell = collider.gameObject;
        }

        private void OnTriggerExit(Collider collider)
        {
            if (!collider.gameObject.CompareTag("Cell"))
                return;

            // Returns original Cell if it's colliding with nothing.
            CellColliding = Cell;
        }

        public void OnPieceDown()
        {
            if (!_playerManager.IsOwner)
                return;

            GameManager.Instance.UpdateChessPieces();

            // Checks if it's your turn to play.
            if (GameManager.Instance.Turn.ToString() != PieceColor.ToString())
                return;

            // Returns true when you hold the piece.
            _isHolding = true;

            // Change cell colors in MovablePieces to orange (it represents cell you can play).
            foreach (var piece in MovableArea)
                piece.GetComponent<MeshRenderer>().material = _playable;

            // Calculates a distance between piece's position and mouse position, I then use it to properly drag object.
            DistanceToMiddle = transform.position - GameManager.Instance.Cam.ScreenToWorldPoint(Input.mousePosition);
            DistanceToMiddle.y = .8f;
        }

        public void OnPieceDrag()
        {
            if (!_playerManager.IsOwner)
                return;

            // Checks if it's your turn to play.
            if (GameManager.Instance.Turn.ToString() != PieceColor.ToString())
                return;

            // Check if there is any play you can make with the piece you're currently holding.
            if (MovableArea.Count == 0)
                return;

            //  Positions the piece to your mouse position.
            var pos = GameManager.Instance.Cam.ScreenToWorldPoint(Input.mousePosition);
            pos.y = PIECE_Y_POSITION;
            transform.position = pos + DistanceToMiddle;
        }

        public void OnPieceUp()
        {
            if (!_playerManager.IsOwner)
                return;

            // Checks if it's your turn to play.
            if (GameManager.Instance.Turn.ToString() != PieceColor.ToString())
                return;

            // Returns false when you stop holding the piece.
            _isHolding = false;

            // Recolor cells after playing a piece: If they are even then they are grey/white, white if black.
            foreach (var piece in MovableArea)
                piece.GetComponent<MeshRenderer>().material = (CharToInt(piece.name[0]) + CharToInt(piece.name[^1])) % 2 == 0 ? _grey : _black;

            // If there is a piece on the cell you want to move, eat that piece first.
            var eatThis = PossiblePiecesYouCanEat.FirstOrDefault(p => p.GetComponent<PieceInfo>().Cell.name == CellColliding.name);
            if (eatThis != null)
            {
                EatPiece(eatThis);
                return;
            }

            // If it's the same cell OR if it's an unmovable cell, then return to original cell.
            if (CellColliding == Cell || !MovableArea.Any(c => c.name == CellColliding.name))
            {
                MoveToCell(true);
                return;
            }

            // If you can play, then do it.
            MovePieceToNewCell(CellColliding);
        }

        #region Moving Related
        // Don't change Cell to anything, just move back to Cell.

        private void EatPiece(GameObject piece)
        {
            var cell = piece.GetComponent<PieceInfo>().Cell;

            GameManager.Instance.EatPieceServerRpc(GameManager.Instance.ChessPieces.IndexOf(piece.GetComponent<PieceInfo>()));

            MovePieceToNewCell(cell);
        }

        private void MovePieceToNewCell(GameObject newCell)
        {
            // Change current Cell variable to new cell you've chosen, then move to that cell.
            Cell = newCell;
            MoveToCell();

            // Change turn.
            GameManager.Instance.ChangeTurnServerRpc(PieceColor);
        }

        public void MovePieceToNewCellWithoutChangingTurn(GameObject newCell)
        {
            // Change current Cell variable to new cell you've chosen, then move to that cell.
            Cell = newCell;
            MoveToCell();
        }

        private async void MoveToCell(bool originalCell = false)
        {
            // Change piece's position.
            var pos = Cell.transform.position;
            pos.y = PIECE_Y_POSITION;
            transform.position = pos;

            if (!originalCell && !_hasMoved)
            {
                _hasMoved = true;

                // A pawn can no longer move 2 cells in one after their first move.
                if (gameObject.name == "Pawn")
                    _maxDistance = 1;
            }

            await Task.Delay(300);

            if (_playerManager.InCheckPosition.Value)
            {
                _playerManager.InCheckPosition.Value = false;
            }
        }
        #endregion

        // þah
        private void CheckOpponent()
        {
            var king = PossiblePiecesYouCanEat.FirstOrDefault(p => p.GetComponent<PieceInfo>().PieceType == PieceType.King);

            if (king != null && !king.transform.parent.GetComponent<PlayerManager>().InCheckPosition.Value)
            {
                print($"{gameObject.name} checks {king.transform.parent.gameObject.name}!");
                var player = king.transform.parent.GetComponent<PlayerManager>();

                player.InCheckPosition.Value = true;
                player.CreateMovableAreaForKingAfterCheck();
            }
        }

        #region Raycast Related
        public void CheckRaycast()
        {
            var t = transform;
            List<Vector3> directionArray = PieceType switch
            {
                PieceType.Pawn => new() { t.forward }, // Piyonla piece yeme kodlayacaðým. piyon köþelere 1 adýmlýk raycast atacak. EÐER ORADA MÝNYON VARSA hareket edebilecek.
                PieceType.Rook => new() { t.forward, -t.forward, -t.right, t.right },
                PieceType.Knight => new() { new Vector3(2.29f, 0, 4.57f), new Vector3(-2.29f, 0, 4.57f), new Vector3(2.29f, 0, -4.57f), new Vector3(-2.29f, 0, -4.57f), new Vector3(4.57f, 0, 2.29f), new Vector3(-4.57f, 0, 2.29f), new Vector3(4.57f, 0, -2.29f), new Vector3(-4.57f, 0, -2.29f) }, // ÖZEL BÝR VECTOR3 YAZACAÐIM BURAYA
                PieceType.Bishop => new() { t.forward + t.right, t.forward - t.right, -t.forward + t.right, -t.forward - t.right },
                PieceType.Queen or PieceType.King => new() { t.forward, -t.forward, t.right, -t.right, t.forward + t.right, t.forward - t.right, -t.forward + t.right, -t.forward - t.right },
                _ => new() { },
            };

            SetMovableAreas(directionArray);
        }

        public void SetEatablePieces(List<Vector3> directions)
        {
            PossiblePiecesYouCanEat.Clear();

            var t = transform;
            if (PieceType == PieceType.Pawn)
                directions = new() { t.forward + t.right, t.forward - t.right };

            foreach (var direction in directions)
            {
                RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, Mathf.Infinity, LayerMask.GetMask(new string[] { "BlackPiece", "WhitePiece" }));
                hits = hits.OrderBy(h => Vector3.Distance(transform.position, h.collider.gameObject.transform.position)).ToArray();

                if (hits.Length == 0)
                    continue;

                var hit = hits[0].collider.gameObject;

                if (GetCellDistance(hit))
                    continue;

                if (hit.transform.parent.name == (t.parent.name == "Black" ? "White" : "Black"))
                {
                    PossiblePiecesYouCanEat.Add(hit);
                    MovableArea.Add(hit.GetComponent<PieceInfo>().Cell);
                }
            }

            CheckOpponent();
        }

        private bool GetCellDistance(GameObject piece)
        {
            var targetCell = piece.GetComponent<PieceInfo>().Cell.name;
            var cell = Cell.name;

            var diffX = Mathf.Abs(targetCell[0] - cell[0]);
            var diffY = Mathf.Abs(targetCell[^1] - cell[^1]);

            if (diffX <= _maxDistance && diffY == 0
                || diffX == 0 && diffY <= _maxDistance
                || diffX <= _maxDistance && diffY == diffX)
                return false;
            else
                return true;
        }

        public void SetMovableAreas(List<Vector3> directions)
        {
            MovableArea.Clear();

            var t = transform;
            foreach (var direction in directions)
            {
                bool ray = Physics.Raycast(t.position, direction, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask($"{(PieceColor == PieceColor.Black ? PieceColor.Black : PieceColor.White)}Piece"));

                RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, Mathf.Infinity, LayerMask.GetMask(new string[] { "BlackPiece", "WhitePiece", "Cell" }));
                hits = hits.OrderBy(h => Vector3.Distance(transform.position, h.collider.gameObject.transform.position)).ToArray();

                if (hits.Length == 0)
                    continue;

                int maxDistance = (_maxDistance > hits.Length ? hits.Length : _maxDistance);
                for (int i = 0; i < maxDistance; i++)
                {
                    GameObject cell = hits[i].collider.gameObject;

                    if (cell.GetComponent<PieceInfo>() != null)
                        break;

                    if (!MovableArea.Any(p => p == cell))
                        MovableArea.Add(cell);
                }
            }

            SetEatablePieces(directions);
        }
        #endregion

        // Simple formula to convert char into int, since I'm working with GameObject names (string).
        private int CharToInt(char c) => c - '0';
    }
}
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon;

public class FieldController : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public GameObject CellPrefab;
    public List<Sprite> CellSprites;

    private Room _room;

    private List<PlayerController> _players;

    private Dictionary<CellName, Sprite> _cellSprites;
    private Cell[,] _field;

    private double _lastCheckWinTime;

    private void Start()
    {
        _room = PhotonNetwork.CurrentRoom;

        _players = new List<PlayerController>();

        _cellSprites = new Dictionary<CellName, Sprite>();
        for (int i = 0; i < CellSprites.Count; i++)
        {
            _cellSprites.Add((CellName)i, CellSprites[i]);
        }

        _field = new Cell[Constants.FieldWidth, Constants.FieldHeight];

        _lastCheckWinTime = PhotonNetwork.Time;
    } 
    private void Update()
    {
        if (_lastCheckWinTime + Constants.CheckWinCoolDown < PhotonNetwork.Time && CheckWin())
        {
            _lastCheckWinTime = PhotonNetwork.Time;

            var eventOptions = new RaiseEventOptions() { Receivers = ReceiverGroup.All };
            var sendOptions = new SendOptions() { Reliability = true };
            PhotonNetwork.RaiseEvent((byte)Event.Win, true, eventOptions, sendOptions);
        }
        _players.ForEach(PlayerPress);
    }

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case (byte)Event.Boom:
                FillEndFieldGraphics();
                _players.ForEach(player => player.gameObject.SetActive(false));
                break;
            case (byte)Event.Win:
                _players.ForEach(player => player.gameObject.SetActive(false));
                break;
            case (byte)Event.AoePress:
                var vectors = ((Vector3[])photonEvent.CustomData).ToList();
                var playerName = _players.Find(player => player.transform.localPosition == vectors.Last()).PhotonView.Owner.NickName;
                vectors.ForEach(vector => _field[(int)vector.x, (int)vector.y].Press(playerName));
                break;
        }
    }

    public void AddPlayer(PlayerController player)
    {
        player.OnSwitchFlag += (x, y) => _field[x, y].SwitchFlag();
        player.OnAoePress += AoePress;

        _players.Add(player);
        if (_room.PlayerCount == _players.Count)
        {
            _players.OrderBy(player => player.PhotonView.Owner.ActorNumber);
            GenerateField();
            InitiateFieldGraphics();
        }
    }

    private void FieldForEach(Action<int, int> method)
    {
        for (int x = 0; x < _field.GetLength(0); x++)
        {
            for (int y = 0; y < _field.GetLength(1); y++)
            {
                method(x, y);
            }
        }
    }

    private void GenerateField()
    {
        var bombs = new List<Vector2Int>();

        while (bombs.Count < Constants.BombCount)
        {
            var position = new Vector2Int(UnityEngine.Random.Range(0, Constants.FieldWidth), UnityEngine.Random.Range(0, Constants.FieldHeight));
            if (!bombs.Exists(bomb => bomb == position)
                && _players.Find(player => PointNeighborhood(position).Exists(point => point == player.GamePosition)) == null)
            {
                _field[position.x, position.y] = new Cell(true);
                bombs.Add(position);
            }
        }

        FieldForEach((x, y) =>
        {
            _field[x, y] ??= new Cell();
            _field[x, y].OnPressed += PressCell;
            _field[x, y].OnFlaged += ToFlagCell;
        });

        FieldForEach((x, y) =>
        {
            var cells = new List<Cell>();
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    if (!(dx == 0 && dy == 0)
                    && ((x + dx) > -1 && (x + dx) < Constants.FieldWidth)
                    && ((y + dy) > -1 && (y + dy) < Constants.FieldHeight))
                    {
                        cells.Add(_field[x + dx, y + dy]);
                    }
                }
            }
            _field[x, y].SetNerbyCells(cells.ToArray());
        });
    }

    private void InitiateFieldGraphics()
    {
        FieldForEach((x, y) => _field[x, y].Controller = Instantiate(CellPrefab, new Vector3(x, y), Quaternion.identity, transform));
    }
    private void FillEndFieldGraphics()
    {
        FieldForEach((x, y) =>
        {
            var cellRender = _field[x, y].Controller.GetComponent<SpriteRenderer>();
            if (cellRender.sprite != _cellSprites[CellName.Boom])
            {
                if (_field[x, y].IsMine && !_field[x, y].IsFlag)
                {
                    cellRender.sprite = _cellSprites[CellName.Bomb];
                }
                if (!_field[x, y].IsMine && _field[x, y].IsFlag)
                {
                    cellRender.sprite = _cellSprites[CellName.NotBomb];
                }
            }
        });
    }

    private void PlayerPress(PlayerController player)
    {
        _field[player.GamePosition.x, player.GamePosition.y].Press(player.PhotonView.Owner.NickName);
    }
    private void PressCell(CellName type, int x, int y, string playerName)
    {
        _field[x, y].Controller.GetComponent<SpriteRenderer>().sprite = _cellSprites[type];
        if (type == CellName.Boom)
        {
            var eventOptions = new RaiseEventOptions() { Receivers = ReceiverGroup.All };
            var sendOptions = new SendOptions() { Reliability = true };
            var eventData = playerName;
            PhotonNetwork.RaiseEvent((byte)Event.Boom, eventData, eventOptions, sendOptions);
        }
    }
    private void ToFlagCell(bool isFlag, int x, int y)
    {
        _field[x, y].Controller.GetComponent<SpriteRenderer>().sprite = isFlag ? _cellSprites[CellName.Flag] : _cellSprites[CellName.Default];
    }

    private void AoePress(int x, int y)
    {
        var AoePressCells = _field[x, y].GetAoePressCells();

        if (AoePressCells.Count > 0)
        {
            var eventOptions = new RaiseEventOptions() { Receivers = ReceiverGroup.All };
            var sendOptions = new SendOptions() { Reliability = true };
            var pressData = AoePressCells.Select(cell => cell.Controller.transform.position).ToList();
            pressData.Add(new Vector3(x, y));
            PhotonNetwork.RaiseEvent((byte)Event.AoePress, pressData.ToArray(), eventOptions, sendOptions);
        }
    }

    private List<Vector2Int> PointNeighborhood(Vector2Int point)
    {
        var result = new List<Vector2Int>();
        for (int dx = -1; dx < 2; dx++)
        {
            for (int dy = -1; dy < 2; dy++)
            {
                result.Add(point + new Vector2Int(dx, dy));
            }
        }
        return result;
    }

    private bool CheckWin()
    {
        var isWin = true;
        FieldForEach((x, y) => isWin &= !_field[x, y].IsMine && _field[x, y].IsPressed || _field[x, y].IsMine && !_field[x, y].IsPressed);
        return isWin;
    }
}
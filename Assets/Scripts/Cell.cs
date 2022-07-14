using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Cell
{
    public GameObject Controller { get; set; }

    public readonly bool IsMine;

    public bool IsFlag { get; private set; }

    private Cell[] _nearbyCells;
    private int _nearbyBombCount => _nearbyCells.Count(cell => cell.IsMine);
    private int _nearbyFlagCount => _nearbyCells.Count(cell => cell.IsFlag);

    public bool IsPressed { get; private set; }

    public Cell(bool isMine = false)
    {
        IsMine = isMine;
        IsFlag = false;
        IsPressed = false;
    }

    public void SetNerbyCells(Cell[] nearbyCells)
    {
        _nearbyCells ??= nearbyCells.ToArray();
    }

    public void Press(string playerName)
    {
        if (IsPressed) return;
        IsPressed = true;

        IsFlag = false;

        var type = IsMine ? CellName.Boom : (CellName)_nearbyBombCount;
        var position = Controller.transform.position;
        OnPressed(type, (int)position.x, (int)position.y, playerName);

        if (_nearbyBombCount == 0 && !IsMine)
        {
            PressNearby(playerName);
        }
    }

    public List<Cell> GetAoePressCells()
    {
        if (_nearbyBombCount != _nearbyFlagCount) return new List<Cell>();

        var isBoom = false;
        _nearbyCells.ToList().ForEach(cell => isBoom |= !((cell.IsFlag && cell.IsMine) || (!cell.IsFlag && !cell.IsMine)));

        return _nearbyCells.Where(cell => isBoom ? cell.IsMine : !cell.IsFlag).ToList();
    }

    public void SwitchFlag()
    {
        if (IsPressed) return;
        IsFlag = !IsFlag;

        var position = Controller.transform.position;
        OnFlaged(IsFlag, (int)position.x, (int)position.y);
    }

    private void PressNearby(string playerName) => _nearbyCells.ToList().ForEach(cell => cell.Press(playerName));
    private void PressNearby(string playerName, IEnumerable<Cell> cells) => cells.ToList().ForEach(cell => cell.Press(playerName));
    private void PressNearby(string playerName, Func<Cell, bool> predicate) => _nearbyCells.Where(predicate).ToList().ForEach(cell => cell.Press(playerName));

    public event Action<CellName, int, int, string> OnPressed;
    public event Action<bool, int, int> OnFlaged;
}

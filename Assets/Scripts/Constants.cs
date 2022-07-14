using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Constants
{
    public static char InvisibleChar = (char)8203;
    public static int FieldWidth = 30;
    public static int FieldHeight = 16;
    public static int BombCount = 99;
    public static double CheckWinCoolDown = 1d;
}

public enum CellName
{
    Empty,
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Bomb,
    Boom,
    Flag,
    Default,
    NotBomb
}

public enum Event
{
    Start,
    Boom,
    End,
    AoePress,
    Win
}

public enum SaveTag
{
    AlternativeRotation
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utilities
{
    public static string SaveTagToString(SaveTag tag) => Enum.GetName(typeof(SaveTag), tag);
}

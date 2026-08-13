using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomData
{
    public int roomId;
    public List<RoomObjectData> roomObjectTemplates = new List<RoomObjectData>();
}
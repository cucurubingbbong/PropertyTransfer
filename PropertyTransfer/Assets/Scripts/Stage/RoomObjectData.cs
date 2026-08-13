using UnityEngine;

[System.Serializable]
public struct RoomObjectData 
{
    public GameObject objectPrefab; 
    public int objectId;
    public Vector3 originPosition;  
    public Quaternion originRotation; 
}
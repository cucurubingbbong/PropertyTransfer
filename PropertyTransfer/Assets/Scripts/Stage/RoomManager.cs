using System.Collections.Generic;
using UnityEngine;


public class RoomManager : MonoBehaviour
{
     public RoomData roomData;

    // 오브젝트마다 ResetState 같은 별도의 인터페이스 구현해줘서 초기화해주기
    private Dictionary<int, GameObject> spawnedObjectsDict = new Dictionary<int, GameObject>();

    void Start()
    {
        GenerateRoom();
    }
    public void GenerateRoom()
    {
        spawnedObjectsDict.Clear();

        for (int i = 0; i < roomData.roomObjectTemplates.Count; i++)
        {
            RoomObjectData data = roomData.roomObjectTemplates[i];
            
            if (data.objectPrefab == null) continue;

            GameObject go = Instantiate(data.objectPrefab, data.originPosition, data.originRotation, this.transform);
            spawnedObjectsDict.Add(i, go);
        }
    }

    public void ResetRoomObjects()
    {
        for (int i = 0; i < roomData.roomObjectTemplates.Count; i++)
        {
            if (spawnedObjectsDict.TryGetValue(i, out GameObject go))
            {
                if (go == null) continue; 

                RoomObjectData originData = roomData.roomObjectTemplates[i];

                go.transform.position = originData.originPosition;
                go.transform.rotation = originData.originRotation;

                if (go.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                go.SetActive(true); 
            }
        }
        
        Debug.Log("방 안의 모든 오브젝트 위치가 초기화되었습니다.");
    }
}

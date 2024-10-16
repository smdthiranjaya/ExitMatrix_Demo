using UnityEngine;

public class PositionTracker : MonoBehaviour
{
    public Transform player;
    public BuildingStructure buildingStructure;

    void Update()
    {
        UpdatePlayerPosition();
    }

    void UpdatePlayerPosition()
    {
        Vector3 playerPos = player.position;
        Room currentRoom = buildingStructure.FindRoomForPosition(playerPos);

        if (currentRoom != null)
        {
            int floorNumber = Mathf.FloorToInt(playerPos.y / buildingStructure.defaultFloorHeight) + 1;
            Debug.Log($"Player is on floor {floorNumber} in {currentRoom.name} at position: {playerPos}");
        }
        else
        {
            Debug.Log($"Player is outside of defined rooms. Position: {playerPos}");
            DebugRoomPositions(playerPos);
        }
    }

    void DebugRoomPositions(Vector3 playerPos)
    {
        Debug.Log("Debugging room positions:");
        foreach (var floor in buildingStructure.floors)
        {
            foreach (var room in floor.rooms)
            {
                Vector3 minBounds = room.position - room.size / 2;
                Vector3 maxBounds = room.position + room.size / 2;
                Debug.Log($"Room: {room.name}, Floor: {floor.floorNumber}, Position: {room.position}, Size: {room.size}");
                Debug.Log($"  Bounds: Min {minBounds}, Max {maxBounds}");
                
                bool inXBounds = playerPos.x >= minBounds.x && playerPos.x <= maxBounds.x;
                bool inYBounds = playerPos.y >= room.position.y && playerPos.y < room.position.y + buildingStructure.defaultFloorHeight;
                bool inZBounds = playerPos.z >= minBounds.z && playerPos.z <= maxBounds.z;
                
                Debug.Log($"  Player in bounds: X: {inXBounds}, Y: {inYBounds}, Z: {inZBounds}");
                Debug.Log($"  Distance to room center: {Vector3.Distance(playerPos, room.position)}");
            }
        }
    }
}
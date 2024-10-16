using UnityEngine;

public class RoomVisualizer : MonoBehaviour
{
    public BuildingStructure buildingStructure;

    void OnDrawGizmos()
    {
        if (buildingStructure == null)
            return;

        if (buildingStructure.floors == null || buildingStructure.floors.Count == 0)
        {
            // If floors are not initialized, call InitializeBuilding
            buildingStructure.SendMessage("InitializeBuilding", SendMessageOptions.DontRequireReceiver);
        }

        foreach (var floor in buildingStructure.floors)
        {
            foreach (var room in floor.rooms)
            {
                Gizmos.color = room.isExit ? Color.green : Color.blue;
                Gizmos.DrawWireCube(room.position, room.size);
            }
        }
    }
}
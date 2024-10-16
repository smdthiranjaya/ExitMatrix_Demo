using UnityEngine;
using System.Collections.Generic;

public class PathfindingGraph : MonoBehaviour
{
    public BuildingStructure buildingStructure;
    private Dictionary<string, Dictionary<string, float>> graph;

    void Start()
    {
        GenerateGraph();
    }

    void GenerateGraph()
    {
        graph = new Dictionary<string, Dictionary<string, float>>();

        foreach (var floor in buildingStructure.floors)
        {
            foreach (var room in floor.rooms)
            {
                string roomKey = $"Floor{floor.floorNumber}_{room.name}";
                graph[roomKey] = new Dictionary<string, float>();

                // Connect to all other rooms on the same floor
                foreach (var otherRoom in floor.rooms)
                {
                    if (room != otherRoom)
                    {
                        string otherRoomKey = $"Floor{floor.floorNumber}_{otherRoom.name}";
                        float distance = Vector3.Distance(room.position, otherRoom.position);
                        graph[roomKey][otherRoomKey] = distance;
                    }
                }

                // Connect to the same room on adjacent floors (simulating stairs/elevators)
                if (floor.floorNumber > 1)
                {
                    string lowerRoomKey = $"Floor{floor.floorNumber - 1}_{room.name}";
                    graph[roomKey][lowerRoomKey] = floor.height; // Assuming height is the distance between floors
                }
                if (floor.floorNumber < buildingStructure.floors.Count)
                {
                    string upperRoomKey = $"Floor{floor.floorNumber + 1}_{room.name}";
                    graph[roomKey][upperRoomKey] = buildingStructure.floors[floor.floorNumber].height;
                }
            }
        }
    }

    public Dictionary<string, float> GetNeighbors(string roomKey)
    {
        return graph.ContainsKey(roomKey) ? graph[roomKey] : new Dictionary<string, float>();
    }

    // Helper method to get room key
    public string GetRoomKey(Vector3 position)
    {
        Room room = buildingStructure.FindRoomForPosition(position);
        if (room != null)
        {
            int floorNumber = Mathf.FloorToInt(position.y / buildingStructure.defaultFloorHeight) + 1;
            return $"Floor{floorNumber}_{room.name}";
        }
        return null;
    }
}
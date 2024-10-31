using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;

public class BuildingStructure : MonoBehaviour
{
    public List<Floor> floors = new List<Floor>();
    public float defaultRoomSize = 5f;
    public float defaultFloorHeight = 3f;
    public Vector3 Ground_Mesh = new Vector3(20f, 0.1f, 20f);
    public Vector3 buildingSize;
    public int mapResolution = 10; // Number of cells per unit
    private Vector3 buildingOffset;

    void Start()
    {
        GenerateAndExportMap();
    }

    void GenerateAndExportMap()
    {
        string json = GenerateJsonOutput();
        string filePath = Path.Combine(Application.persistentDataPath, "building_map.json");
        File.WriteAllText(filePath, json);
        Debug.Log($"Map exported to: {filePath}");
    }


    public void CalculateBuildingSize()
    {
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        foreach (var floor in floors)
        {
            foreach (var room in floor.rooms)
            {
                Vector3 roomMin = room.position - room.size / 2;
                Vector3 roomMax = room.position + room.size / 2;

                min = Vector3.Min(min, roomMin);
                max = Vector3.Max(max, roomMax);
            }
        }

        buildingSize = max - min;
        buildingOffset = -min;
    }


    public List<string[][]> Generate2DMaps()
    {
        CalculateBuildingSize();

        List<string[][]> allMaps = new List<string[][]>();

        int width = Mathf.CeilToInt(buildingSize.x * mapResolution);
        int height = Mathf.CeilToInt(buildingSize.z * mapResolution);

        for (int floorIndex = 0; floorIndex < floors.Count; floorIndex++)
        {
            string[][] map = new string[height][];
            for (int i = 0; i < height; i++)
            {
                map[i] = new string[width];
                for (int j = 0; j < width; j++)
                {
                    map[i][j] = ".";
                }
            }

            Floor floor = floors[floorIndex];
            for (int i = 0; i < floor.rooms.Count; i++)
            {
                Room room = floor.rooms[i];
                Vector3 adjustedPosition = room.position + buildingOffset;
                int startX = Mathf.Max(0, Mathf.FloorToInt((adjustedPosition.x - room.size.x / 2) * mapResolution));
                int startZ = Mathf.Max(0, Mathf.FloorToInt((adjustedPosition.z - room.size.z / 2) * mapResolution));
                int endX = Mathf.Min(width - 1, Mathf.CeilToInt((adjustedPosition.x + room.size.x / 2) * mapResolution));
                int endZ = Mathf.Min(height - 1, Mathf.CeilToInt((adjustedPosition.z + room.size.z / 2) * mapResolution));

                string roomName = room.name.ToUpper();

                for (int z = startZ; z <= endZ; z++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        if (x == startX || x == endX || z == startZ || z == endZ)
                        {
                            map[z][x] = "#"; // Wall
                        }
                        else if (z == startZ + 1 && x >= startX + 1 && x < startX + 1 + roomName.Length)
                        {
                            map[z][x] = roomName[x - (startX + 1)].ToString();
                        }
                        else if (room.isExit)
                        {
                            map[z][x] = "E"; // Mark exits with 'E'
                        }
                        else
                        {
                            map[z][x] = " "; // Empty space inside room
                        }
                    }
                }
            }

            allMaps.Add(map);
        }

        return allMaps;
    }

    public string GenerateJsonOutput()
    {
        StringBuilder json = new StringBuilder();
        json.Append("{\n");
        json.Append($"  \"Ground_Mesh\": {{\"x\": {Ground_Mesh.x}, \"y\": {Ground_Mesh.y}, \"z\": {Ground_Mesh.z}}},\n");
        json.Append($"  \"Building_Size\": {{\"x\": {buildingSize.x}, \"y\": {buildingSize.y}, \"z\": {buildingSize.z}}},\n");
        
        List<string[][]> allMaps = Generate2DMaps();
        json.Append("  \"Floors\": [\n");
        for (int floorIndex = 0; floorIndex < floors.Count; floorIndex++)
        {
            Floor floor = floors[floorIndex];
            json.Append("    {\n");
            json.Append($"      \"Floor_Number\": {floor.floorNumber},\n");
            
            // Add 2D map to JSON
            string[][] map = allMaps[floorIndex];
            json.Append("      \"2D_Map\": [\n");
            for (int i = 0; i < map.Length; i++)
            {
                json.Append("        \"").Append(string.Join("", map[i])).Append("\"");
                if (i < map.Length - 1) json.Append(",");
                json.Append("\n");
            }
            json.Append("      ],\n");

            // Add rooms information
            json.Append("      \"Rooms\": [\n");
            for (int i = 0; i < floor.rooms.Count; i++)
            {
                Room room = floor.rooms[i];
                json.Append("        {\n");
                json.Append($"          \"Name\": \"{room.name}\",\n");
                json.Append($"          \"Position\": {{\"x\": {room.position.x}, \"y\": {room.position.y}, \"z\": {room.position.z}}},\n");
                json.Append($"          \"Size\": {{\"x\": {room.size.x}, \"y\": {room.size.y}, \"z\": {room.size.z}}},\n");
                json.Append($"          \"IsExit\": {room.isExit.ToString().ToLower()}\n");
                json.Append("        }");
                if (i < floor.rooms.Count - 1) json.Append(",");
                json.Append("\n");
            }
            json.Append("      ]\n");
            json.Append("    }");
            if (floorIndex < floors.Count - 1) json.Append(",");
            json.Append("\n");
        }
        json.Append("  ]\n");
        json.Append("}");

        return json.ToString();
    }

    public void UpdatePlayerPosition(Vector3 playerPosition)
    {
        Room currentRoom = FindRoomForPosition(playerPosition);
        int currentFloor = Mathf.FloorToInt(playerPosition.y / defaultFloorHeight);

        Debug.Log($"Player Position: {{\"x\": {playerPosition.x}, \"y\": {playerPosition.y}, \"z\": {playerPosition.z}}}");
        Debug.Log($"Current Floor: {currentFloor + 1}");
        Debug.Log($"Current Room: {(currentRoom != null ? currentRoom.name : "Outside")}");
    }

    public Room FindRoomForPosition(Vector3 position)
    {
        foreach (var floor in floors)
        {
            foreach (var room in floor.rooms)
            {
                if (IsPositionInRoom(position, room))
                    return room;
            }
        }
        return null;
    }

    private bool IsPositionInRoom(Vector3 position, Room room)
    {
        Vector3 minBounds = room.position - room.size / 2;
        Vector3 maxBounds = room.position + room.size / 2;
        return position.x >= minBounds.x && position.x <= maxBounds.x &&
               position.z >= minBounds.z && position.z <= maxBounds.z &&
               position.y >= room.position.y && position.y < room.position.y + defaultFloorHeight;
    }


}

[System.Serializable]
public class Room
{
    public string name;
    public Vector3 position;
    public Vector3 size;
    public bool isExit;
}

[System.Serializable]
public class Floor
{
    public int floorNumber; // Add this line
    public List<Room> rooms;
    public float height;
}
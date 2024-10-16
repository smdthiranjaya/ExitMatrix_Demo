using UnityEngine;
using System.Collections.Generic;

public class BuildingStructure : MonoBehaviour
{
    public List<Floor> floors = new List<Floor>();
    public float defaultRoomSize = 5f;
    public float defaultFloorHeight = 3f;

    void Awake()
    {
        if (floors.Count == 0)
        {
            InitializeBuilding();
        }
    }

    void OnValidate()
    {
        if (floors.Count == 0)
        {
            InitializeBuilding();
        }
    }

    void InitializeBuilding()
    {
        floors = new List<Floor>
        {
            new Floor
            {
                floorNumber = 1,
                height = defaultFloorHeight,
                rooms = new List<Room>
                {
                    new Room { name = "Room 1", position = new Vector3(0, 0, 0), size = Vector3.one * defaultRoomSize },
                    new Room { name = "Room 2", position = new Vector3(6, 0, 0), size = Vector3.one * defaultRoomSize },
                    new Room { name = "Exit", position = new Vector3(3, 0, 6), size = Vector3.one * defaultRoomSize, isExit = true }
                }
            },
            new Floor
            {
                floorNumber = 2,
                height = defaultFloorHeight,
                rooms = new List<Room>
                {
                    new Room { name = "Room 3", position = new Vector3(0, defaultFloorHeight, 0), size = Vector3.one * defaultRoomSize },
                    new Room { name = "Room 4", position = new Vector3(6, defaultFloorHeight, 0), size = Vector3.one * defaultRoomSize },
                    new Room { name = "Exit", position = new Vector3(3, defaultFloorHeight, 6), size = Vector3.one * defaultRoomSize, isExit = true }
                }
            }
        };
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
    public int floorNumber;
    public List<Room> rooms;
    public float height;
}
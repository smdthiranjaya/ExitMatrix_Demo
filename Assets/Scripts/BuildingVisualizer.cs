using UnityEngine;

public class BuildingVisualizer : MonoBehaviour
{
    public BuildingStructure buildingStructure;
    public GameObject roomPrefab;
    public GameObject exitPrefab;
    public float roomSize = 5f;
    public float floorHeight = 3f;

    void Start()
    {
        VisualizeBuildingStructure();
    }

    void VisualizeBuildingStructure()
    {
        foreach (var floor in buildingStructure.floors)
        {
            foreach (var room in floor.rooms)
            {
                Vector3 position = room.position + new Vector3(0, floor.floorNumber * floorHeight, 0);
                GameObject roomObj = Instantiate(room.isExit ? exitPrefab : roomPrefab, position, Quaternion.identity, transform);
                roomObj.name = $"Floor{floor.floorNumber}_{room.name}";
                roomObj.transform.localScale = Vector3.one * roomSize;
            }
        }
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;

public class PositionTracker : MonoBehaviour
{
    public Transform player;
    public BuildingStructure buildingStructure;
    public float updateInterval = 3f; // Update interval in seconds
    private float lastUpdateTime;

    public List<Transform> firePositions; // Assign this in the inspector or update dynamically

    private const string ApiUrl = "http://localhost:5000/navigate"; // Update this URL to match your API server

    void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdatePlayerPosition();
            lastUpdateTime = Time.time;
        }
    }

    void UpdatePlayerPosition()
    {
        Vector3 playerPos = player.position;
        buildingStructure.UpdatePlayerPosition(playerPos);

        Room currentRoom = buildingStructure.FindRoomForPosition(playerPos);

        if (currentRoom != null)
        {
            int floorNumber = Mathf.FloorToInt(playerPos.y / buildingStructure.defaultFloorHeight) + 1;
            Debug.Log($"Player is on floor {floorNumber} in {currentRoom.name} at position: {playerPos}");

            // Call the navigation API
            StartCoroutine(GetNavigationInstructions(floorNumber, currentRoom.name, playerPos));
        }
        else
        {
            Debug.Log($"Player is outside of defined rooms. Position: {playerPos}");
            DebugRoomPositions(playerPos);
        }
    }

    [System.Serializable]
    private class Position
    {
        public string x;
        public string y;
        public string z;
    }

    [System.Serializable]
    private class NavigationRequest
    {
        public int current_floor;
        public string current_room;
        public Position player_position;
        public List<Position> fire_positions;
    }

    [System.Serializable]
    private class NavigationResponse
    {
        public List<List<int>> path;
        public List<string> instructions;
        public List<int> exit_position;
    }

    [System.Serializable]
    private class ErrorResponse
    {
        public string error;
    }

    IEnumerator GetNavigationInstructions(int currentFloor, string currentRoom, Vector3 playerPosition)
    {
        // Create the request body
        var request = new NavigationRequest
        {
            current_floor = currentFloor,
            current_room = currentRoom,
            player_position = new Position
            {
                x = playerPosition.x.ToString("G17"),
                y = playerPosition.y.ToString("G17"),
                z = playerPosition.z.ToString("G17")
            },
            fire_positions = new List<Position>()
        };

        // Add fire positions
        foreach (var fireTransform in firePositions)
        {
            request.fire_positions.Add(new Position
            {
                x = fireTransform.position.x.ToString("G17"),
                y = fireTransform.position.y.ToString("G17"),
                z = fireTransform.position.z.ToString("G17")
            });
        }

        string jsonBody = JsonUtility.ToJson(request);
        Debug.Log("Sending request: " + jsonBody);

        using (UnityWebRequest webRequest = new UnityWebRequest(ApiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string response = webRequest.downloadHandler.text;
                Debug.Log("Received navigation instructions: " + response);

                NavigationResponse navigationData = JsonUtility.FromJson<NavigationResponse>(response);
                if (navigationData != null && navigationData.instructions != null)
                {
                    DisplayInstructions(navigationData.instructions);
                }
                else
                {
                    Debug.LogError("Failed to parse navigation response");
                }
            }
            else
            {
                Debug.LogError("Error getting navigation instructions: " + webRequest.error);
                Debug.LogError("Response Code: " + webRequest.responseCode);
                string errorBody = webRequest.downloadHandler.text;
                Debug.LogError("Response Body: " + errorBody);

                // Try to parse the error message
                ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(errorBody);
                if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.error))
                {
                    Debug.LogError("Server Error: " + errorResponse.error);
                }
            }
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

    void DisplayInstructions(List<string> instructions)
    {
        if (instructions == null || instructions.Count == 0)
        {
            Debug.LogWarning("No navigation instructions received");
            return;
        }

        Debug.Log("Navigation Instructions:");
        foreach (var instruction in instructions)
        {
            Debug.Log(instruction);
        }
    }

    [System.Serializable]
    private class Wrapper
    {
        public Dictionary<string, object> data;
        public Wrapper(Dictionary<string, object> data) { this.data = data; }
    }
}
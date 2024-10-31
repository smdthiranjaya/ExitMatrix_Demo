using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

public class NavigationClient : MonoBehaviour
{
    private const string ApiUrl = "http://localhost:5000/navigate";

    public IEnumerator GetNavigationInstructions(int currentFloor, string currentRoom, Vector3 playerPosition, List<Vector3> firePositions)
    {
        // Create the request body
        var requestBody = new Dictionary<string, object>
        {
            ["current_floor"] = currentFloor,
            ["current_room"] = currentRoom,
            ["player_position"] = new Dictionary<string, string>
            {
                ["x"] = playerPosition.x.ToString("G17", CultureInfo.InvariantCulture),
                ["y"] = playerPosition.y.ToString("G17", CultureInfo.InvariantCulture),
                ["z"] = playerPosition.z.ToString("G17", CultureInfo.InvariantCulture)
            },
            ["fire_positions"] = firePositions.ConvertAll(pos => new Dictionary<string, string>
            {
                ["x"] = pos.x.ToString("G17", CultureInfo.InvariantCulture),
                ["y"] = pos.y.ToString("G17", CultureInfo.InvariantCulture),
                ["z"] = pos.z.ToString("G17", CultureInfo.InvariantCulture)
            })
        };

        string jsonBody = JsonUtility.ToJson(requestBody);

        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(ApiUrl, jsonBody))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                // Parse and use the response here
                Debug.Log("Received response: " + response);
                // You would typically parse this JSON and use it to guide the player
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }
    }
}
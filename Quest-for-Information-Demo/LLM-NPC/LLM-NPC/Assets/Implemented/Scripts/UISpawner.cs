using UnityEngine;

/// <summary>
/// Spawns the keyboard object in front of the player in lobby.
/// </summary>
public class UISpawner : MonoBehaviour
{
    [SerializeField] 
    private GameObject _keyboardObject;

    [SerializeField] 
    private Transform _playerTransform;

    [SerializeField] 
    private float _distanceFromPlayer = 1f;

    [SerializeField]
    private float _yOffset = 0f;


    public void SpawnUI()
    {
        if (_keyboardObject == null || _playerTransform == null)
        {
            Debug.LogWarning("KeyboardPrefab or PlayerTransform not set!");
            return;
        }

        Vector3 spawnPosition = _playerTransform.position + _playerTransform.forward * _distanceFromPlayer + _yOffset * Vector3.up;
        _keyboardObject.transform.position = spawnPosition;
        _keyboardObject.transform.rotation = Quaternion.LookRotation(-_playerTransform.forward);
        _keyboardObject.SetActive(true);
    }
}

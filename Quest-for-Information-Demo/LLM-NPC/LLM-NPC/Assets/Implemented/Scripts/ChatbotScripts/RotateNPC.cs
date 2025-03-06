using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateNPC : MonoBehaviour
{
    [SerializeField]
    private Transform _head;

    [SerializeField] 
    private Transform _body;

    [SerializeField]
    private Transform _player;

    private int _maxAngle = 45;

    private float _rotationSpeed = 5.0f;

    private void Update()
    {
        if (_player == null) return;

        Vector3 directionToPlayer = (_player.position - _body.position);
        directionToPlayer.y = 0;
        Quaternion targetHeadRotation = Quaternion.LookRotation(directionToPlayer);
        Vector3 headEuler = targetHeadRotation.eulerAngles;
        targetHeadRotation = Quaternion.Euler(0, headEuler.y, 0);

        Quaternion relativeHeadRotation = Quaternion.Euler(0, _head.localEulerAngles.y, 0);
        float headAngle = Quaternion.Angle(Quaternion.identity, relativeHeadRotation);

        if (headAngle <= _maxAngle)
        {
            _head.rotation = Quaternion.Slerp(_head.rotation, targetHeadRotation, _rotationSpeed * Time.deltaTime);
        }
        else
        {
            Quaternion targetBodyRotation = Quaternion.LookRotation(directionToPlayer);
            targetBodyRotation = Quaternion.Euler(0, targetBodyRotation.eulerAngles.y + 180, 0);

            // Smoothly rotate the body towards the player
            _body.rotation = Quaternion.Slerp(_body.rotation, targetBodyRotation, _rotationSpeed * Time.deltaTime);

            // Smoothly rotate the head towards the player
            _head.rotation = Quaternion.Slerp(_head.rotation, targetHeadRotation, _rotationSpeed * Time.deltaTime);
        }
    }
}

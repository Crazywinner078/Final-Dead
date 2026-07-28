using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 第一人称鼠标视角。水平旋转玩家身体，垂直旋转 CameraRoot 并限制抬头低头角度。
/// </summary>
public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float minpitch = -75f;
    [SerializeField] private float maxpitch = 75f;

    private float pitch;

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX=Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        // 左右看时旋转整个 Player，这样移动方向也会跟着视角转。
        transform.Rotate(Vector3.up * mouseX);


        // 上下看只旋转 CameraRoot，避免整个 CharacterController 前后倾斜。
        pitch = Mathf.Clamp(pitch - mouseY, minpitch, maxpitch);
        cameraRoot.localEulerAngles = new Vector3(pitch, 0, 0);
    }
}

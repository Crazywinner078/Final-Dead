using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

/// <summary>
/// 第一人称玩家移动。只负责 WASD 平面移动和重力，不处理镜头、交互或跳跃。
/// </summary>
public class PlayerMotor : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -18f;

    private CharacterController controller;
    private Vector3 velocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }
    private void Update()
    {
        float x = UnityEngine.Input.GetAxis("Horizontal");
        float z = UnityEngine.Input.GetAxis("Vertical");

        // 移动方向跟随玩家朝向：right 控制横移，forward 控制前后。
        Vector3 move = transform.right * x + transform.forward * z;
        if (move.sqrMagnitude > 1f)
            move.Normalize();
        controller.Move(move * moveSpeed * Time.deltaTime);

        // CharacterController 不会自动处理重力，所以这里手动累计竖直速度。
        if(controller.isGrounded&&velocity.y<0f)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}

using UnityEngine;
using UnityEngine.InputSystem; // �VInput System

public class DualSenseRayShooter : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private float maxWireLength = 30f; // �ő咷
    [SerializeField] private float extendSpeed = 20f;   // �L�т鑬��
    [SerializeField] private int curveSegments = 20;    // ���_���i�J�[�u���炩���j

    private float currentLength = 0f;
    private bool isExtending = false;
    private Vector3 targetPoint;

    void Update()
    {
        if (Gamepad.current != null)
        {
            if (Gamepad.current.rightTrigger.wasPressedThisFrame)
            {
                StartWire();
            }

            if (isExtending)
            {
                ExtendWire();
            }
        }
    }

    private void StartWire()
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        // �q�b�g����
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxWireLength, hitLayers))
        {
            Debug.Log("���C�̃q�b�g");
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = origin + direction * maxWireLength;
        }

        currentLength = 0f;
        isExtending = true;
        lineRenderer.positionCount = curveSegments;
        lineRenderer.enabled = true;
    }

    private void ExtendWire()
    {
        currentLength += extendSpeed * Time.deltaTime;
        float totalLength = Vector3.Distance(playerCamera.transform.position, targetPoint);

        if (currentLength >= totalLength)
        {
            currentLength = totalLength;
            isExtending = false; // ���B������L�яI��
        }

        // �n�_�ƌ��݂̐�[�ʒu
        Vector3 start = playerCamera.transform.position;
        Vector3 end = Vector3.Lerp(start, targetPoint, currentLength / totalLength);

        // ���C���[�̃J�[�u�v�Z
        for (int i = 0; i < curveSegments; i++)
        {
            float t = (float)i / (curveSegments - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);

            // ���Ԃ���������܂���i���C���[���j
            float sag = Mathf.Sin(t * Mathf.PI) * 0.2f; // �|�Ȃ�
            pos.y -= sag;

            // �����h���ǉ�
            float sway = Mathf.Sin(Time.time * 10f + t * 5f) * 0.02f;
            pos.x += sway;

            lineRenderer.SetPosition(i, pos);
        }
    }
    private void OnDrawGizmos()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * maxWireLength);
        }
    }

}

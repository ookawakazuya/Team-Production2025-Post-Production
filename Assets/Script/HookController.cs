using UnityEngine;
using UnityEngine.InputSystem;

public class DualSenseWireController : MonoBehaviour
{
    [Header("�v���C���[�֘A")]
    [SerializeField] private Camera playerCamera;   // �v���C���[���_�p�J����

    [Header("���C���[�֘A�ݒ�")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private float maxWireLength = 15f;
    [SerializeField] private float extendSpeed = 20f;
    [SerializeField] private float retractSpeed = 25f;
    [SerializeField] private int curveSegments = 20;

    private bool wireMode = false;
    private bool isExtending = false;
    private bool isRetracting = false;

    private Vector3 targetPoint;
    private float currentLength;

    void Update()
    {
        if (Gamepad.current == null) return;

        // ================================
        // �~�{�^���Ń��C���[���[�h�ؑ�
        // ================================
        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            wireMode = !wireMode;
            Debug.Log($"���C���[���[�h: {(wireMode ? "ON" : "OFF")}");
        }

        if (!wireMode)
        {
            lineRenderer.enabled = false;
            return;
        }

        // ================================
        // R2�Ŏˏo�J�n
        // ================================
        if (Gamepad.current.rightTrigger.wasPressedThisFrame)
        {
            StartWire();
        }

        // ================================
        // ���C���[�L�я���
        // ================================
        if (isExtending && !isRetracting && Gamepad.current.rightTrigger.isPressed)
        {
            ExtendWire();
        }

        // ================================
        // R2�����������
        // ================================
        if (Gamepad.current.rightTrigger.wasReleasedThisFrame)
        {
            ReleaseWire();
        }

        // ================================
        // R1��������u������胂�[�h�J�n�v
        // �i�L�ђ��ł����������ɐؑցj
        // ================================
        if (Gamepad.current.rightShoulder.wasPressedThisFrame && lineRenderer.enabled)
        {
            isExtending = false;   // �L�т�̂𒆒f
            isRetracting = true;   // �������J�n
        }

        // ================================
        // ������胂�[�h���Ȃ玩���������
        // ================================
        if (isRetracting)
        {
            RetractWire();
        }
    }

    private void StartWire()
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxWireLength, hitLayers))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = origin + direction * maxWireLength;
        }

        currentLength = 0f;
        isExtending = true;
        isRetracting = false;

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
            isExtending = false; // ���R�ɐL�ѐ؂�����I��
        }

        DrawWire(playerCamera.transform.position, Vector3.Lerp(playerCamera.transform.position, targetPoint, currentLength / totalLength));
    }

    private void ReleaseWire()
    {
        lineRenderer.enabled = false;
        isExtending = false;
        isRetracting = false;
    }

    private void RetractWire()
    {
        if (!lineRenderer.enabled) return;

        currentLength -= retractSpeed * Time.deltaTime;

        if (currentLength <= 0f)
        {
            currentLength = 0f;
            ReleaseWire(); // ���S�Ɋ�����肫���������
            return;
        }

        DrawWire(playerCamera.transform.position, Vector3.Lerp(playerCamera.transform.position, targetPoint, currentLength / Vector3.Distance(playerCamera.transform.position, targetPoint)));
    }

    private void DrawWire(Vector3 start, Vector3 end)
    {
        for (int i = 0; i < curveSegments; i++)
        {
            float t = (float)i / (curveSegments - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);

            float sag = Mathf.Sin(t * Mathf.PI) * 0.2f;
            pos.y -= sag;

            float sway = Mathf.Sin(Time.time * 10f + t * 5f) * 0.02f;
            pos.x += sway;

            lineRenderer.SetPosition(i, pos);
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public class HookContlore : MonoBehaviour
{
    [SerializeField] private Camera playerCamera; // �v���C���[���_�J����
    [SerializeField] private LineRenderer lineRenderer; // ���C�����p
    [SerializeField] private LayerMask hitLayers; // �q�b�g����Ώہi�ǂȂǁj

    private void Update()
    {
        if (Gamepad.current != null)
        {
            // R2�̉������ݗʁi0.0�`1.0�j
            float r2 = Gamepad.current.rightTrigger.ReadValue();

            if (r2 > 0.1f) // �����ł�������Ă�����
            {
                ShootRay();
            }
            else
            {
                lineRenderer.enabled = false; // �g���K�[�𗣂������\��
            }
        }
    }

    private void ShootRay()
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        Ray ray = new Ray(origin, direction);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, hitLayers))
        {
            // ���C��ǂ܂ŕ\��
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, hit.point);
        }
        else
        {
            // ������Ȃ�������100m��܂ŕ\��
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, origin + direction * 100f);
        }
    }
}

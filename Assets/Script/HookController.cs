using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class DualSenseWireController : MonoBehaviour
{
    [Header("プレイヤー関連")]
    [SerializeField] private Camera playerCamera;   // プレイヤー視点用カメラ
    [SerializeField] private CharacterController characterController; // プレイヤーの移動制御用
    [SerializeField] private Transform originalCameraParent; // 通常時のカメラの親
    [SerializeField] private Transform moveCameraParent; // 移動中のカメラの親

    [Header("ワイヤー関連設定")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LayerMask hookableLayers; // フック可能なレイヤー
    [SerializeField] private LayerMask interactiveLayers; // ギミック用レイヤー
    [SerializeField] private float maxWireLength = 15f;
    [SerializeField] private float extendSpeed = 20f;
    [SerializeField] private float retractSpeed = 25f;
    [SerializeField] private float moveSpeed = 30f; // 移動用フックの移動速度
    [SerializeField] private int curveSegments = 20;

    // 共通
    private bool canShootHook = true;
    private float coolDownTime = 1f;

    // 移動用フック
    private bool isGrappling = false;
    private bool isReturning = false;
    private Vector3 grapplePoint;
    private Vector3 lastPosition;
    private Coroutine stayOnWallCoroutine;

    // ギミック用フック
    private Transform grabbedObject;

    void Update()
    {
        if (Gamepad.current == null) return;

        // ×ボタンでワイヤーモード切替
        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            // ギミック用と移動用のフックモードを切り替えるロジックを実装
            // 例: public enum HookMode { Grapple, Gimmick }
            Debug.Log("フックモード切り替え");
        }

        // R2でフックを射出
        if (Gamepad.current.rightTrigger.wasPressedThisFrame && canShootHook)
        {
            StartCoroutine(ShootHook());
        }

        // R2を離したらフックを解除
        if (Gamepad.current.rightTrigger.wasReleasedThisFrame)
        {
            ReleaseHook();
        }

        // R1押したら「巻き取りモード開始」
        if (Gamepad.current.rightShoulder.wasPressedThisFrame)
        {
            if (isGrappling)
            {
                isReturning = true;
            }
            if (grabbedObject != null)
            {
                // ギミックを巻き取る
                StartCoroutine(RetractObject(grabbedObject));
            }
        }
    }

    private IEnumerator ShootHook()
    {
        canShootHook = false;
        RaycastHit hit;
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        // フックが壁、側面、足場、またはギミックに当たったか判定
        if (Physics.Raycast(origin, direction, out hit, maxWireLength, hookableLayers | interactiveLayers))
        {
            // ギミック用フック
            if (interactiveLayers == (interactiveLayers | (1 << hit.collider.gameObject.layer)))
            {
                Debug.Log("ギミックに当たった");
                grabbedObject = hit.transform;
                // ここでオブジェクトの掴む処理を開始
            }
            // 移動用フック
            else
            {
                Debug.Log("移動用フックが当たった");
                grapplePoint = hit.point;
                lastPosition = transform.position; // 戻るための位置を保存
                isGrappling = true;

                // 移動中のカメラ処理
                playerCamera.transform.SetParent(moveCameraParent);
                // 移動モーションやカメラの引きをここで実装
            }
        }
        else
        {
            Debug.Log("何にも当たらなかった");
        }

        // クールタイム
        yield return new WaitForSeconds(coolDownTime);
        canShootHook = true;
    }

    void LateUpdate()
    {
        // 移動用フックの移動処理
        if (isGrappling)
        {
            Vector3 newPosition = Vector3.MoveTowards(transform.position, grapplePoint, moveSpeed * Time.deltaTime);
            characterController.Move(newPosition - transform.position);

            // 目標地点に到達したか判定
            if (Vector3.Distance(transform.position, grapplePoint) < 1f)
            {
                isGrappling = false;
                // ここで壁に留まる処理を開始
                stayOnWallCoroutine = StartCoroutine(StayOnWall(5f));
            }
        }
        else if (isReturning)
        {
            Vector3 newPosition = Vector3.MoveTowards(transform.position, lastPosition, moveSpeed * Time.deltaTime);
            characterController.Move(newPosition - transform.position);

            if (Vector3.Distance(transform.position, lastPosition) < 1f)
            {
                isReturning = false;
                ReleaseHook();
            }
        }

        // ワイヤーの描画処理
        if (isGrappling || isReturning || grabbedObject != null)
        {
            lineRenderer.enabled = true;
            // 描画ロジックをここに
            DrawWire(playerCamera.transform.position, isGrappling || isReturning ? grapplePoint : grabbedObject.position);
        }
    }

    private void ReleaseHook()
    {
        lineRenderer.enabled = false;
        isGrappling = false;
        isReturning = false;
        grabbedObject = null;
        if (stayOnWallCoroutine != null)
        {
            StopCoroutine(stayOnWallCoroutine);
        }

        // カメラを元の状態に戻す
        playerCamera.transform.SetParent(originalCameraParent);
    }

    private IEnumerator StayOnWall(float stayTime)
    {
        // 壁に留まっている間、プレイヤーの移動を無効化
        // characterController.enabled = false;
        yield return new WaitForSeconds(stayTime);
        // characterController.enabled = true;
        // 5秒経過後、フックを強制解除
        ReleaseHook();
    }

    private IEnumerator RetractObject(Transform obj)
    {
        // オブジェクトを引き寄せる処理
        // 例: obj.position = Vector3.MoveTowards(...)
        yield return null;
    }

    private void DrawWire(Vector3 start, Vector3 end)
    {
        // 既存の描画ロジックを流用
        // ...
    }
}
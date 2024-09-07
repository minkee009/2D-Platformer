using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform Player;

    public float yOffset = 2f;

    public float lerpMotionSpeed = 3f;

    public float minLerpSpeed = 2f;
    public float maxLerpSpeed = 15f;

    public Vector3 currentPos = Vector3.zero;
    public Vector3 targetPos = Vector3.zero;

    public float depthX = 2f;
    public float depthY = 1f;

    public float deadzoneX = 2f;
    public float deadzoneY = 1.5f;
    
    const float DEADZONE_THRESHOLD = 2f;

    // Start is called before the first frame update
    void Start()
    {
        targetPos = Player.position;
        currentPos = Player.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (Mathf.Abs(targetPos.x - Player.position.x) > depthX)
            targetPos.x = Player.position.x > targetPos.x ? Player.position.x - depthX : Player.position.x + depthX;

        if (Mathf.Abs(targetPos.y - Player.position.y) > depthY)
            targetPos.y = Player.position.y > targetPos.y ? Player.position.y - depthY : Player.position.y + depthY;

        var setXLerpSpeed = minLerpSpeed + ((maxLerpSpeed - minLerpSpeed) * Mathf.Clamp01(Mathf.Max(0f, Mathf.Abs(Player.position.x - currentPos.x) - deadzoneX) / DEADZONE_THRESHOLD));
        var setYLerpSpeed = minLerpSpeed + ((maxLerpSpeed - minLerpSpeed) * Mathf.Clamp01(Mathf.Max(0f , Mathf.Abs(Player.position.y - currentPos.y) - deadzoneY) / DEADZONE_THRESHOLD));

        currentPos.x = Mathf.Lerp(currentPos.x, targetPos.x, 1 - Mathf.Exp(-setXLerpSpeed * Time.deltaTime));
        currentPos.y = Mathf.Lerp(currentPos.y, targetPos.y, 1 - Mathf.Exp(-setYLerpSpeed * Time.deltaTime));

        //currentPos = Vector3.Lerp(currentPos, targetPos, lerpMotionSpeed * Time.deltaTime);

        transform.position = currentPos + Vector3.up * yOffset + Vector3.forward * -10f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 1);
        Gizmos.DrawLine(transform.position + new Vector3 { x = depthX, y = (-yOffset + depthY), z = 10 }, transform.position + new Vector3 { x = depthX, y = (-yOffset - depthY), z = 10 });
        Gizmos.DrawLine(transform.position + new Vector3 { x = depthX, y = (-yOffset - depthY), z = 10 }, transform.position + new Vector3 { x = -depthX, y = (-yOffset - depthY), z = 10 });
        Gizmos.DrawLine(transform.position + new Vector3 { x = -depthX, y = (-yOffset - depthY), z = 10 }, transform.position + new Vector3 { x = -depthX, y = (-yOffset + depthY), z = 10 });
        Gizmos.DrawLine(transform.position + new Vector3 { x = -depthX, y = (-yOffset + depthY), z = 10 }, transform.position + new Vector3 { x = depthX, y = (-yOffset + depthY), z = 10 });

        Gizmos.color = new Color(1, 0.5f, 0.75f, 1);
        Gizmos.DrawLine(transform.position + new Vector3 { x = deadzoneX, y = (-yOffset + deadzoneY), z = 10 }, transform.position + new Vector3 { x = deadzoneX, y = (-yOffset - deadzoneY), z = 10});
        Gizmos.DrawLine(transform.position + new Vector3 { x = deadzoneX, y = (-yOffset - deadzoneY), z = 10 }, transform.position + new Vector3 { x = -deadzoneX, y = (-yOffset - deadzoneY), z = 10});
        Gizmos.DrawLine(transform.position + new Vector3 { x = -deadzoneX, y = (-yOffset - deadzoneY), z = 10 }, transform.position + new Vector3 { x = -deadzoneX, y = (-yOffset + deadzoneY), z = 10 });
        Gizmos.DrawLine(transform.position + new Vector3 { x = -deadzoneX, y = (-yOffset + deadzoneY), z = 10 }, transform.position + new Vector3 { x = deadzoneX, y = (-yOffset + deadzoneY), z = 10 });

        Gizmos.color = new Color(1, 0f, 1f);
        Gizmos.DrawWireCube(transform.position + new Vector3 { y = -yOffset }, new Vector3(deadzoneX * 2f + DEADZONE_THRESHOLD * 2f, deadzoneY * 2f + DEADZONE_THRESHOLD * 2, 1));

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(targetPos, new Vector3 (depthX * 2f, depthY * 2f, 1));
    }
}

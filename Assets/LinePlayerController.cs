using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LinePlayerController : MonoBehaviour, ILineMoveObj
{
    public Vector2 ProjectPoint { get => transform.position; }
    public Vector2 Velocity { get => _velocity; set => _velocity = value; }

    public SpriteRenderer spriteRender;

    public float speed = 5f;
    public float gravity = 5f;
    public float jumpforce = 10f;
    public float drag = 0.2f;
    public float accel = 0.65f;
    public float accel_air = 0.015f;
    public int max_additional_airjump_count = 3;

    public bool isGrounded;

    public BBox extend;

    public Camera main;

    public LineGround DebugGround;

    private Vector2 _velocity;
    private Vector2 _groundNormal;
    private float _lastYvel;

    private Vector2 _internalPos;
    private Vector2 _lastFixedPos;
    private Vector2 _lastInternalPos;

    private bool _lastMoveHitLine;

    private IList<Line> _collisionLInes;
    private Line[] _validLines = new Line[MAX_SEARCHCOUNT];
    private int _validLineCount;

    //private IList<Line> _debugGround;

    public float hInput = 0;
    public float vInput = 0;

    const int MAX_MOVEITERATION = 4;
    const int MAX_SEARCHCOUNT = 8;
    const float SWEEPTEST_OFFSET = 0.0002f;
    const float COLLISION_OFFSET = 0.0004f;
    const float LEDGE_CHECK_OFFSET = 0.001f;

    private bool _forceUnground = false;
    private float _ungroundTime = 0.0f;

    private bool _jumpInput = false;
    private bool _jumpHoldInput = false;
    private float _lastHInput = 0;
    int _jumpCount = 0;

    private void Awake()
    {
        FixedInterpolator.CreateInstance();
    }

    public Vector2 initPos;

    // Start is called before the first frame update
    void Start()
    {
        initPos = ProjectPoint;
        _internalPos = ProjectPoint;
        _lastInternalPos = ProjectPoint;
        //if (DebugGround != null)
        //    _debugGround = DebugGround.CreateGround();

        //디버그 용 땅 생성 후 바인딩
        if (DebugGround != null)
            SetCollisionLines(DebugGround.CreateGround());

        Line de = new Line(new Vector2(3.5f, -3.5f), new Vector2(5f, -3f));
        //Line de = new Line(new Vector2(3.9f, -3.865f), new Vector2(5f, -3.6f));

        de.frontNormal = new Vector2(-de.ToVector.y, de.ToVector.x);

        DebugGround.lines.AddLine(de);

        //그라운드 스냅핑
        //ProbeGround();

        main = Camera.main;

        //groundProject
        //foreach(var line in _debugGround)
        //{
        //    if (ProjectPoint.x >= line.start.x && ProjectPoint.x <= line.end.x)
        //    {
        //        if (CustomPhysics.Raycast2D(ProjectPoint, Vector2.down, 25f, line, out CustomRayCastHit2D hitInfo))
        //        {
        //            transform.position = hitInfo.hitPoint;
        //            Debug.DrawRay(transform.position, hitInfo.normal);
        //            break;
        //        }

        //        //var lineDir = (line.max - line.min).normalized;
        //        //var posVec = ProjectPoint - line.min;
        //        //float dotProduct = Vector2.Dot(posVec, lineDir);

        //        //transform.position = line.min + lineDir * dotProduct;

        //        //라인 스윕(레이캐스팅)으로 전환
        //    }
        //}
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt) || (Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false))
            _jumpInput = true;


        _jumpHoldInput = Input.GetKey(KeyCode.LeftAlt) || (Gamepad.current?.buttonSouth.isPressed ?? false);

        hInput = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) + (Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f);
        vInput = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) + (Input.GetKey(KeyCode.DownArrow) ? -1f : 0f);

        hInput += Mathf.Abs(Gamepad.current?.leftStick.x.value ?? 0.0f) > 0.4f ? Mathf.Sign(Gamepad.current?.leftStick.x.value ?? 0.0f) : 0f;
        vInput += Mathf.Abs(Gamepad.current?.leftStick.y.value ?? 0.0f) > 0.4f ? Mathf.Sign(Gamepad.current?.leftStick.y.value ?? 0.0f) : 0f;

        hInput = Mathf.Clamp(hInput, -1, 1);

        if (Input.GetKeyDown(KeyCode.V))
            MustUnground(0.1f);

        var currentPos = Vector2.Lerp(_lastFixedPos, _internalPos, FixedInterpolator.instance.InterpolationFactor);

        transform.position = currentPos;

        if (Input.GetKeyDown(KeyCode.G))
            DebugMode = !DebugMode;

        if (DebugMode)
        {
            Line l = new Line(ProjectPoint, main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward * 1.5f));

            bool fact = LineSweepTest(ProjectPoint, (l.end - l.start).normalized, (l.end - l.start).magnitude + COLLISION_OFFSET, out CustomRayCastHit2D dd, SWEEPTEST_OFFSET);

            Debug.DrawLine(l.start, l.end, Color.magenta);
            DrawBBoxMono(BBox.LineToBBox(l), Color.white);

            if (fact)
                Debug.DrawRay(dd.hitPoint, dd.normal, Color.blue);

            if (fact && Input.GetMouseButtonDown(0))
            {
                dd.distance = Mathf.Max(0.0f, dd.distance - COLLISION_OFFSET);

                _internalPos = ProjectPoint + l.ToVector.normalized * dd.distance;
                _lastFixedPos = _internalPos;
                transform.position = _internalPos;

                isGrounded = false;
                ProbeGround();
            }
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            _velocity = Vector3.zero;
            _internalPos = initPos;
            _lastFixedPos = initPos;
            transform.position = _internalPos;
        }    
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _lastFixedPos = _internalPos;

        _ungroundTime = Mathf.Max(0.0f,_ungroundTime - Time.deltaTime);

        if (_ungroundTime > 0)
            _forceUnground = true;
        else
            _forceUnground = false;

        if(!DebugMode && _internalPos.y < -20.0f)
        {
            _velocity = Vector3.zero;
            _internalPos = initPos;
            _lastFixedPos = initPos;
            transform.position = _internalPos;
        }

        Move();

        _lastHInput = hInput;
        Debug.DrawRay(_internalPos, Velocity / Time.deltaTime, Color.magenta);
        if (!isGrounded && _velocity.y > 0.0f && !_lastMoveHitLine)
            return;


        var lastGround = isGrounded;
        ProbeGround();

        if (!lastGround && isGrounded)
        {
            //_velocity = Vector2.ClampMagnitude(_velocity, speed);
            //var crossNormal = new Vector2(-_groundNormal.y, _groundNormal.x);
            //var projectVel = Vector2.Dot(Vector2.down * gravity, crossNormal) * crossNormal;

            //_velocity.y = 0.0f;
            //_velocity.x += projectVel.x / Time.deltaTime;

            //Debug.Log($"vel: +{projectVel.x}");
        }
    }


    public void Move()
    {
        _lastInternalPos = _internalPos;
        var deltaTime = Time.fixedDeltaTime;

        if(hInput != 0.0f && _lastHInput != hInput)
        {
            spriteRender.flipX = hInput > 0 ? true : false;    
        }

        if (isGrounded && !_forceUnground)
        {
            var crossNormal = new Vector2(-_groundNormal.y, _groundNormal.x);
            _velocity = Mathf.Sign(Vector2.Dot(_velocity, crossNormal)) * crossNormal * _velocity.magnitude;
            _jumpCount = 0;

            if (hInput != 0f && _velocity.magnitude < speed || MathF.Sign(_velocity.x) != hInput)
            {
                var currentDir = MathF.Sign(Vector2.Dot(crossNormal, Vector2.right * hInput)) * crossNormal;

                _velocity += currentDir * accel * deltaTime;
                //_velocity += Vector2.down * gravity * Time.deltaTime;

                //_velocity.x = Mathf.Min(Mathf.Abs(_velocity.x), speed) * Mathf.Sign(_velocity.x);
            }

            if(_velocity.sqrMagnitude > 0f)
            {
                if (hInput == 0f)
                    _velocity = _velocity.normalized * (Mathf.Max(_velocity.magnitude - (drag * deltaTime), 0.0f));
                else if (hInput != 0f && _velocity.magnitude > speed)
                    _velocity = _velocity.normalized * Mathf.Max(speed, _velocity.magnitude - (drag * deltaTime));

            }
            //            _velocity = Vector2.ClampMagnitude(_velocity, speed);


            //    //Debug.DrawRay(hitInfo.hitPoint, crossNormal, Color.cyan, 0.5f);



            if (_jumpHoldInput)
            {
                _jumpInput = false;
                _velocity.y = 0f;
                _velocity += Vector2.up * jumpforce;

                if(hInput != 0)
                {
                    var projectPlaneNormal = new Vector2(_groundNormal.y, -_groundNormal.x);
                    var gravitySlideForce = Vector2.Dot(Vector2.down * gravity * 5f, projectPlaneNormal);
                    gravitySlideForce = (projectPlaneNormal * gravitySlideForce).x * deltaTime;


                    _velocity.x = hInput * speed + gravitySlideForce;
                }

                //if (hInput != 0.0f && Mathf.Sign(_velocity.x) != Mathf.Sign(hInput))
                //    _velocity.x = hInput * accel * Time.deltaTime;
                MustUnground(0.05f);
            }

        }
        else
        {

            if ( (speed * 0.2f >= Mathf.Abs(Velocity.x)) || Mathf.Sign(Velocity.x) != hInput)
            {
                _velocity.x += hInput * accel_air * deltaTime;
            }
            //else
            //{
            //    _velocity += -_velocity.normalized * airDrag * deltaTime;
            //}



            if (_jumpCount < max_additional_airjump_count && _jumpInput)
            {
                var faceDir = spriteRender.flipX ? 1 : -1;

                _jumpCount++;
                _jumpInput = false;
                _velocity.y = 0f;
                _velocity += Vector2.up * jumpforce * 0.8f;
                _velocity.x = 0f;
                _velocity.x += faceDir * jumpforce * 1.2f;
                //if (hInput != 0.0f && Mathf.Sign(_velocity.x) != Mathf.Sign(hInput))
                //    _velocity.x = hInput * accel * Time.deltaTime;
                MustUnground(0.05f);
            }

            else if (_velocity.y > -speed * 8f)
            {
                _velocity += Vector2.down * gravity * deltaTime;

                _jumpInput = false;
            }
                
        }

        _lastMoveHitLine = CollideAndSlide(_internalPos, ref _velocity, out Vector2 outPos);

        _internalPos = outPos;
        //땅인경우와 공중인경우 제작
        //땅인경우 먼저 땅의 기울기 파악후 입력 방향을 그 기울기에 맞게 투영
        //그이후 이터레이션을 통해 콜라이드 앤 슬라이드
    }

    public bool CollideAndSlide(Vector2 startPosition, ref Vector2 initVelocity, out Vector2 outPos)
    {
        var currentVel = initVelocity;
        var tmpPosition = startPosition;
        var hitInfo = new CustomRayCastHit2D();
        bool hitAnyLine = false;
        bool hitGround = false;

        //if (initVelocity.magnitude <= SWEEPTEST_OFFSET)
        //{
        //    initVelocity = Vector2.zero;
        //    outPos = tmpPosition;
        //    return;
        //}

      

        for (int i = 0; i < MAX_MOVEITERATION; i++)
        {
            if (LineSweepTest(tmpPosition, currentVel.normalized, currentVel.magnitude + COLLISION_OFFSET, out hitInfo, SWEEPTEST_OFFSET))
            {
                //Debug.Log($"[{i}] : 습테 통과");

                //if(_velocity.y > 0 && hitInfo.normal.y < -0.7)
                //{
                //    tmpPosition += currentVel;
                //    break;
                //}    
               

                hitInfo.distance = Mathf.Max(0.0f, hitInfo.distance - COLLISION_OFFSET);


                tmpPosition += currentVel.normalized * hitInfo.distance;
                currentVel += currentVel.normalized * -hitInfo.distance;

                var crossNormal = new Vector2(-hitInfo.normal.y, hitInfo.normal.x);

                //Debug.DrawRay(hitInfo.hitPoint, crossNormal, Color.cyan, 0.5f);

                currentVel = Vector2.Dot(currentVel, crossNormal) * crossNormal;
                initVelocity = Vector2.Dot(initVelocity, crossNormal) * crossNormal;

                if (!hitGround && !isGrounded && hitInfo.normal.y > 0.5f)
                {
                    hitGround = true;
                    initVelocity = initVelocity.normalized * Mathf.Min(speed * 2f,initVelocity.magnitude);
                    if (hInput != 0f && MathF.Sign(initVelocity.x) != hInput)
                    {
                        initVelocity = Vector2.zero;
                        currentVel = Vector2.zero;
                        
                    }
                    isGrounded = true;
                    ProbeGround(false);

                    //    initVelocity = crossNormal
                }

                hitAnyLine = true;

                //Debug.Log(currentVel.magnitude);

                //Debug.DrawRay(tmpPosition, currentVel, Color.black, 12f);
            }
            else
            {
                //Debug.Log($"[{i}] : 비통과 , {currentVel}");

                tmpPosition += currentVel;// + currentVel * SWEEPTEST_OFFSET;
                break;
            }

            if (currentVel.magnitude <= 0.0f || Vector2.Dot(initVelocity.normalized, currentVel) < 0f)
            {
                currentVel = Vector2.zero;
                break;
            }
        }

        //if(currentVel.sqrMagnitude > 0.0f && hitAnyLine)
        //{
        //    //오버랩
        //    //탐색방향 결정
        //    var velDir = currentVel.normalized;
        //    var crossProduct = CustomPhysics.Cross2D(Vector2.up, velDir);
        //    var overlapDirIsVert = crossProduct < 0.7f && crossProduct >= -0.7f;
        //    var overlapDir = overlapDirIsVert ? Vector2.up : Vector2.right;

        //    if(LineSweepTest(tmpPosition + overlapDir * OVERLAPTEST_OFFSET, -overlapDir, OVERLAPTEST_OFFSET * 2f, out CustomRayCastHit2D overlapInfo, 0, false)
        //        && overlapInfo.hitLine.hasFrontNormal && Vector2.Dot(overlapInfo.hitLine.frontNormal,tmpPosition - overlapInfo.hitPoint) < 0)
        //    {
        //        tmpPosition = overlapInfo.hitPoint;
        //    }
        //}

        //if (hitAnyLine)
        //{
        //    var curVelDir = currentVel.normalized;
        //    initVelocity = curVelDir * initVelocity.magnitude * Vector2.Dot(initVelocity.normalized, curVelDir);
        //}
        outPos = tmpPosition;
        return hitAnyLine;
    }

    public void MustUnground(float time = 0.1f)
    {
        _ungroundTime += time;
        isGrounded = false;
    }

    public void SetCollisionLines(IList<Line> collisionLines)
    {
        _collisionLInes = collisionLines;
    }

    public void ProbeGround(bool checkLedge = true)
    {
        bool checkState = isGrounded;

        float checkDist = checkState ? 0.5f : 0.025f;
        float checkHeight = 0.025f;
        isGrounded = false;
        _groundNormal = Vector2.up;

        var checkPos = _internalPos + Vector2.up * checkHeight;

        if (LineSweepTest(checkPos, Vector2.down, checkDist + COLLISION_OFFSET, out CustomRayCastHit2D hit, SWEEPTEST_OFFSET))
        {
            Debug.DrawRay(checkPos, Vector2.down * checkDist, Color.cyan);
            hit.distance = Mathf.Max(0.0f, hit.distance - COLLISION_OFFSET);

            var probedPos = checkPos + Vector2.down * hit.distance;
            if (checkLedge && _velocity.x != 0)
            {
                Vector2 dir = (_velocity.y > 0 ? Vector2.left : Vector2.right) * Mathf.Sign(probedPos.x - _lastInternalPos.x);
                bool isHitWall = LineSweepTest((_velocity.y > 0 ? _lastInternalPos : probedPos) + (Mathf.Abs(_lastInternalPos.y - probedPos.y) * 0.5f * Vector2.up) + (dir * LEDGE_CHECK_OFFSET), -dir, Mathf.Abs(probedPos.x - _lastInternalPos.x) + LEDGE_CHECK_OFFSET, out CustomRayCastHit2D wallHit);
                bool isLedge = hit.distance > 0.025f + LEDGE_CHECK_OFFSET && (!isHitWall || wallHit.normal.y <= 0) && (probedPos.y - _lastInternalPos.y < 0);

                //if (isHitWall)
                //    Debug.DrawRay(wallHit.hitPoint, wallHit.normal, Color.blue, 0.45f);

                if (isLedge)
                {
                    if(_velocity.y > 0 
                        && LineSweepTest(probedPos + (Mathf.Abs(_lastInternalPos.y - probedPos.y) * 0.5f * Vector2.up) + (-dir * LEDGE_CHECK_OFFSET), dir, Mathf.Abs(probedPos.x - _lastInternalPos.x) + LEDGE_CHECK_OFFSET, out wallHit)
                        && wallHit.normal.y > 0f)
                    {
                        isGrounded = true;
                        _internalPos = probedPos;
                        _groundNormal = hit.normal;
                        return;
                    }

                    //Debug.DrawRay(probedPos, hit.normal, Color.cyan, 0.45f);
                    //Debug.DrawRay(_lastInternalPos, Vector2.up, Color.red, 0.45f);
                    isGrounded = false;
                    return;
                }
            }

            isGrounded = true;
            _internalPos = probedPos;
            _groundNormal = hit.normal;
        }
    }

    public bool LineSweepTest(Vector2 position, Vector2 direction, float dist, out CustomRayCastHit2D closestHit, float sweepOffset = 0, bool checkFrontNormal = true)
    {
        if (sweepOffset != 0)
        {
            sweepOffset = Mathf.Max(0.0f, sweepOffset);
        }
        closestHit = new CustomRayCastHit2D();
        var searchLine = new Line(position + direction * -sweepOffset, (direction * (dist + sweepOffset)) + position);
        if (!SearchValidLines(BBox.LineToBBox(searchLine), out int searchCount))
        {
            return false;
        }

        int validCount = searchCount;

        var closestDist = Mathf.Infinity;

        for (int i = 0; i < searchCount; i++)
        {
            var line = _validLines[i];

            if ((checkFrontNormal && line.hasFrontNormal && Vector2.Dot(direction, line.frontNormal) > 0)
                || CustomPhysics.Cross2D(line.ToVector.normalized, direction) == 0)
            {
                validCount--;
                continue;
            }
            if (CustomPhysics.Raycast2D(position + direction * -sweepOffset, direction, dist + sweepOffset, line, out CustomRayCastHit2D currentHit))
            {
                //디버그 - sweep bias 처리 생각해보기
                if (currentHit.distance < sweepOffset)
                {
                    validCount--;
                    continue;
                }

                if (currentHit.distance < closestDist)
                {
                    closestDist = currentHit.distance;
                    closestHit = currentHit;
                }
            }
            else
            {
                validCount--;
            }
        }

        closestHit.distance -= sweepOffset;

        return validCount > 0 ? true : false;
    }

    public bool DebugMode;

    public bool SearchValidLines(BBox validArea, out int validCount)
    {
        System.Array.Clear(_validLines, 0, MAX_SEARCHCOUNT);

        validCount = 0;
        int searchIndex = -1;
        foreach (Line line in _collisionLInes)
        {
            searchIndex++;
            var lineBox = BBox.LineToBBox(line);

            if( lineBox.min.x <= validArea.max.x &&
                lineBox.max.x >= validArea.min.x &&
                lineBox.min.y <= validArea.max.y &&
                lineBox.max.y >= validArea.min.y)
            {
                _validLines[validCount++] = line;
            }

            if (validCount == MAX_SEARCHCOUNT)
                break;
        }

        return validCount != 0;
    }

    //private void OnDrawGizmos()
    //{
    //    var transExtendMin = extend.min + new Vector2(transform.position.x, transform.position.y);
    //    var transExtendMax = extend.max + new Vector2(transform.position.x, transform.position.y);

    //    DrawBBox(new BBox(transExtendMin, transExtendMax), Color.green);

    //    if(_validLines != null)
    //        foreach(var line in _validLines)
    //        {
    //            DrawBBox(BBox.LineToBBox(line), Color.yellow);
    //        }
    //}

    private void DrawBBox(BBox box, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(new Vector2(box.min.x, box.min.y), new Vector2(box.max.x, box.min.y));
        Gizmos.DrawLine(new Vector2(box.max.x, box.min.y), new Vector2(box.max.x, box.max.y));
        Gizmos.DrawLine(new Vector2(box.max.x, box.max.y), new Vector2(box.min.x, box.max.y));
        Gizmos.DrawLine(new Vector2(box.min.x, box.max.y), new Vector2(box.min.x, box.min.y));
    }

    private void DrawBBoxMono(BBox box, Color color) 
    {
        Debug.DrawLine(new Vector2(box.min.x, box.min.y), new Vector2(box.max.x, box.min.y), color);
        Debug.DrawLine(new Vector2(box.max.x, box.min.y), new Vector2(box.max.x, box.max.y), color);
        Debug.DrawLine(new Vector2(box.max.x, box.max.y), new Vector2(box.min.x, box.max.y), color);
        Debug.DrawLine(new Vector2(box.min.x, box.max.y), new Vector2(box.min.x, box.min.y), color);
    }
}

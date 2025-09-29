using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;

public class LineObjMove : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    public float speed = 5f;
    public float gravity = 5f;
    public float jumpforce = 10f;
    public float drag = 0.2f;
    public float accel = 0.65f;
    public float accel_air = 0.015f;

    private int _currentGroundID = -1;
    private Vector2 _internalPos;
    private Vector2 _lastFixedPos;
    private Vector2 _lastInternalPos;
    private float _distance;
    private Vector2 _velocity;
    private int _segIDX;

    private float _hInput;
    private float _vInput;
    private float _lastHInput;
    private bool _jumpHoldInput;
    private bool _isGrounded;

    private void Awake()
    {
        FixedInterpolator.CreateInstance();
        if(!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        FoundNewGround();
        _internalPos = transform.position;
        _lastInternalPos = _internalPos;
    }

    void FoundNewGround()
    {
        float groundY = float.MinValue;
        foreach(var ground in LineGround.LineGroundMap.Values)
        {
            foreach(var line in ground.footholds)
            {
                var currentY = 0.0f;
                BelowGround(_internalPos, line, out currentY, out float dist);

                if (_internalPos.x >= line.start.x 
                    && _internalPos.x <= line.end.x 
                    && _internalPos.y >= currentY 
                    && currentY > groundY)
                {
                    groundY = currentY;
                    _currentGroundID = ground.GetInstanceID();
                    _segIDX = line.index;
                }
            }
        }
    }

    void FoundGround()
    {
        if(_currentGroundID == -1 || !LineGround.LineGroundMap.ContainsKey(_currentGroundID))
        {
            return;
        }

        float groundY = float.MinValue;
        foreach (var line in LineGround.LineGroundMap[_currentGroundID].footholds)
        {
            var currentY = 0.0f;
            BelowGround(_internalPos, line, out currentY,out float dist);

            if (_internalPos.x >= line.start.x
                && _internalPos.x <= line.end.x
                && _internalPos.y >= currentY
                && currentY > groundY)
            {
                groundY = currentY;
                _segIDX = line.index;
            }
        }
    }


    void BelowGround(Vector2 pos, Line line, out float y, out float dist)
    {
        var start = line.start;
        var end = line.end;

        // 계산의 편의를 위해 x좌표 기준으로 start, end 정렬
        if (start.x > end.x)
        {
            var temp = start;
            start = end;
            end = temp;
        }

        // 선분의 전체 길이 미리 계산
        var lineLength = (line.end - line.start).magnitude;

        // posX를 선분의 x범위 내로 제한 (Clamp)
        var clampedX = Mathf.Clamp(pos.x, start.x, end.x);

        // 선형 보간 비율(t) 계산
        var t = (clampedX - start.x) / (end.x - start.x);

        // 1. t를 이용해 y 높이 계산
        y = Mathf.Lerp(start.y, end.y, t);

        // 2. t를 이용해 경사면을 따른 거리(dist) 계산
        dist = lineLength * t;

        if(line.start.x <= pos.x && line.end.x >= pos.x)
        {
            Debug.DrawLine(line.start, line.end, Color.green);
            Debug.DrawLine(pos, line.CalcPosFromDistance(dist), Color.yellow);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 입력
        if (Input.GetKeyDown(KeyCode.F))
        {
            _internalPos = new Vector2(_internalPos.x, _internalPos.y + 5.0f);
            _velocity = Vector2.zero;
            _currentGroundID = -1;
            _isGrounded = false;
            FoundNewGround();
        }

        _jumpHoldInput = Input.GetKey(KeyCode.LeftAlt) || (Gamepad.current?.buttonSouth.isPressed ?? false);

        _hInput = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) + (Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f);
        _vInput = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) + (Input.GetKey(KeyCode.DownArrow) ? -1f : 0f);

        _hInput += Mathf.Abs(Gamepad.current?.leftStick.x.value ?? 0.0f) > 0.4f ? Mathf.Sign(Gamepad.current?.leftStick.x.value ?? 0.0f) : 0f;
        _vInput += Mathf.Abs(Gamepad.current?.leftStick.y.value ?? 0.0f) > 0.4f ? Mathf.Sign(Gamepad.current?.leftStick.y.value ?? 0.0f) : 0f;

        _hInput = Mathf.Clamp(_hInput, -1, 1);

        var currentPos = Vector2.Lerp(_lastFixedPos, _internalPos, FixedInterpolator.instance.InterpolationFactor);
        transform.position = currentPos;
    }

    void FixedUpdate()
    {


        if (_hInput != 0.0f && _lastHInput != _hInput)
        {
            spriteRenderer.flipX = _hInput > 0 ? true : false;
        }

        var deltaTime = Time.fixedDeltaTime;
        _lastFixedPos = _internalPos;
        _lastInternalPos = _internalPos;

        var groundSeg = _currentGroundID != -1 && LineGround.LineGroundMap.ContainsKey(_currentGroundID) ? LineGround.LineGroundMap[_currentGroundID].lines : null;
        if(groundSeg == null || groundSeg.Count == 0)
        {
            //공중 움직임 처리
            _isGrounded = false;
            InAirMove(deltaTime);
            return;
        }

        if (_isGrounded)
        {
            GroundMove(deltaTime);
        }
        else
        {
            InAirMove(deltaTime);
        }

        
        _lastHInput = _hInput;

        Debug.DrawRay(_internalPos + Vector2.up * 0.125f, _velocity, Color.red, 0.1f);
    }

    void CalcDistInSegs(LineSegments segs)
    {
        //음수인 경우
        if (_distance < 0f)
        {
            while (_distance < 0f && _segIDX >= 0)
            {
                //탈출
                if (_segIDX == 0 ||
                    (Mathf.Approximately(0.0f, segs.segment[_segIDX - 1].frontNormal.y) && segs.segment[_segIDX - 1].frontNormal.x < 0.0f))
                {
                    _internalPos = segs.segment[_segIDX].CalcPosFromDistance(_distance);
                    _isGrounded = false;
                    break;
                }

                //현재 라인이 벽인 경우
                var nextFrontNormal = segs.segment[_segIDX - 1].frontNormal;
                if (Mathf.Approximately(0.0f, nextFrontNormal.y) && nextFrontNormal.x > 0.0f)
                {
                    _distance = 0;
                    _velocity = Vector2.zero;
                    break;
                }

                _segIDX--;
                _distance += segs.segment[_segIDX].ToVector.magnitude;
            }
        }
        else if (_distance > segs.segment[_segIDX].ToVector.magnitude)
        {
            while (_distance > segs.segment[_segIDX].ToVector.magnitude && _segIDX <= segs.Count - 1)
            {
                //탈출
                if (_segIDX == segs.Count - 1 ||
                    (Mathf.Approximately(0.0f, segs.segment[_segIDX + 1].frontNormal.y) && segs.segment[_segIDX + 1].frontNormal.x > 0.0f))
                {
                    _internalPos = segs.segment[_segIDX].CalcPosFromDistance(_distance);
                    _isGrounded = false;
                    break;
                }

                //현재 라인이 벽인 경우
                var nextFrontNormal = segs.segment[_segIDX + 1].frontNormal;
                if (Mathf.Approximately(0.0f, nextFrontNormal.y) && nextFrontNormal.x < 0.0f)
                {
                    _distance = segs.segment[_segIDX].ToVector.magnitude;
                    _velocity = Vector2.zero;
                    break;
                }

                _distance -= segs.segment[_segIDX].ToVector.magnitude;
                _segIDX++;
            }
        }
    }

    void GroundMove(float deltaTime)
    {
        var groundSeg = LineGround.LineGroundMap[_currentGroundID].lines;

        //목표 속력 -> 중력의 분력을 고려한 속도
        var groundDir = groundSeg.segment[_segIDX].ToVector.normalized;
        var gravityForceVec = Vector2.Dot(Vector2.down * gravity, groundDir) * groundDir * 0.125f;
        var targetSpeed = speed + (Mathf.Abs(gravityForceVec.x) * (Mathf.Sign(_velocity.x) == Mathf.Sign(gravityForceVec.x) ? 1f : -0.55f));
        targetSpeed = Mathf.Clamp(targetSpeed, speed * 0.5f, speed * 1.5f);

        //가속 적용
        if (_hInput != 0f && _velocity.magnitude < targetSpeed || MathF.Sign(_velocity.x) != _hInput)
        {
            _velocity += Vector2.right * (_hInput * accel) * deltaTime;
        }

        //마찰 적용
        if (_velocity.sqrMagnitude > 0f)
        {
            if (_hInput == 0f)
                _velocity = _velocity.normalized * (Mathf.Max(_velocity.magnitude - (drag * deltaTime), 0.0f));
            else if (_hInput != 0f && _velocity.magnitude > targetSpeed)
                _velocity = _velocity.normalized * Mathf.Max(targetSpeed, _velocity.magnitude - (drag * deltaTime));
        }

        //점프 확인
        if (_jumpHoldInput)
        {
            _velocity.y = 0f;
            _velocity += Vector2.up * jumpforce;

            if (_hInput != 0)
            {
                var projectPlaneNormal = new Vector2(groundSeg.segment[_segIDX].frontNormal.y, -groundSeg.segment[_segIDX].frontNormal.x);
                var gravitySlideForce = Vector2.Dot(Vector2.down * gravity * 5f, projectPlaneNormal);
                gravitySlideForce = (projectPlaneNormal * gravitySlideForce).x * deltaTime;

                _velocity.x = _hInput * speed + gravitySlideForce;
            }

            _isGrounded = false;
            InAirMove(deltaTime);
            return;
        }

        _distance += Mathf.Sign(_velocity.x) * _velocity.magnitude * deltaTime;

        //세그먼트 이동
        CalcDistInSegs(groundSeg);

        _internalPos = groundSeg.segment[_segIDX].CalcPosFromDistance(_distance);
    }

    void InAirMove(float deltaTime)
    {
        if ((speed * 0.2f >= Mathf.Abs(_velocity.x)) || Mathf.Sign(_velocity.x) != _hInput)
        {
            _velocity.x += _hInput * accel_air * deltaTime;
        }
        if (_velocity.y > -speed * 8f)
        {
            _velocity += Vector2.down * gravity * deltaTime;
        }

        //충돌 검사
        //새로운 땅 찾기
        FoundNewGround();
        var groundSeg = LineGround.LineGroundMap.ContainsKey(_currentGroundID) ? LineGround.LineGroundMap[_currentGroundID].lines : null;
        if (groundSeg == null || groundSeg.Count == 0)
        {
            _internalPos += _velocity * deltaTime;
            return;
        }

        var isRight = _velocity.x > 0f;
        var currentPos = _internalPos;
        var nextPos = _internalPos + _velocity * deltaTime;
        var nextSegIDX = isRight ? Mathf.Min(_segIDX + 1, groundSeg.Count - 1) : Mathf.Max(_segIDX - 1, 0);

        //벽 검사
        if (nextSegIDX != _segIDX)
        {
            var nextLine = groundSeg.segment[nextSegIDX];
            if (Mathf.Approximately(nextLine.frontNormal.y,0.0f))
            {
                //벽 충돌
                bool isCollide = false;
                Vector2 collisionPoint = Vector2.zero;

                if (isRight)
                {
                    // 오른쪽 이동 시 벽과의 교차점 계산
                    float wallX = nextLine.start.x; // 벽의 X 좌표

                    if (currentPos.x <= wallX && nextPos.x >= wallX)
                    {

                        // 이동 경로와 벽이 교차하는 Y 좌표 계산
                        float t = (wallX - currentPos.x) / (nextPos.x - currentPos.x);
                        t = Mathf.Clamp01(t); // 0~1로 제한
                        float intersectY = Mathf.Lerp(currentPos.y, nextPos.y, t);

                        // 벽의 Y 범위 내에서 충돌하는지 확인
                        float wallMinY = Mathf.Min(nextLine.start.y, nextLine.end.y);
                        float wallMaxY = Mathf.Max(nextLine.start.y, nextLine.end.y);

                        if (intersectY >= wallMinY && intersectY <= wallMaxY)
                        {
                            _internalPos.x = wallX; // 살짝 떨어뜨려서 겹침 방지
                            collisionPoint = new Vector2(wallX, intersectY);
                            isCollide = true;
                        }
                    }
                }
                else
                {
                    // 왼쪽 이동 시 벽과의 교차점 계산
                    float wallX = nextLine.end.x; // 벽의 X 좌표

                    if (currentPos.x >= wallX && nextPos.x <= wallX)
                    {
                        float t = (currentPos.x - wallX) / (currentPos.x - nextPos.x);
                        t = Mathf.Clamp01(t);
                        float intersectY = Mathf.Lerp(currentPos.y, nextPos.y, t);

                        float wallMinY = Mathf.Min(nextLine.start.y, nextLine.end.y);
                        float wallMaxY = Mathf.Max(nextLine.start.y, nextLine.end.y);

                        if (intersectY >= wallMinY && intersectY <= wallMaxY)
                        {
                            _internalPos.x = wallX; // 살짝 떨어뜨려서 겹침 방지
                            collisionPoint = new Vector2(wallX, intersectY);
                            isCollide = true;
                        }
                    }
                }

                if (isCollide)
                {
                    // 충돌 지점에서 Y 좌표도 조정
                    nextPos.x = _internalPos.x;
                    _velocity.x = 0f;
                }
            }
        }


        //땅 검사
        if (_velocity.y <= 0.0f)
        {
            BelowGround(nextPos, groundSeg.segment[_segIDX], out float belowY, out float dist);
            if (nextPos.y <= belowY)
            {
                _isGrounded = true;
                _distance = dist;

                //경사에서 미끄러짐
                var groundNormal = groundSeg.segment[_segIDX].frontNormal;
                var crossNormal = new Vector2(-groundNormal.y, groundNormal.x);
                var projectVel = Vector2.Dot(Vector2.down * -_velocity.y, crossNormal) * crossNormal;
                if (_hInput == 0f)
                {
                    _velocity.x += projectVel.x;
                    _distance += projectVel.x * deltaTime;
                }

                Debug.Log(dist);
                Debug.DrawRay(groundSeg.segment[_segIDX].CalcPosFromDistance(dist), groundSeg.segment[_segIDX].frontNormal, Color.blueViolet, 0.5f);

                CalcDistInSegs(groundSeg);
                _internalPos = groundSeg.segment[_segIDX].CalcPosFromDistance(_distance);

                _velocity.y = 0f;
                return;
            }
        }

        _internalPos = nextPos;
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;

public class LineObjMove : MonoBehaviour
{
    public LineGround currentGround;
    public SpriteRenderer spriteRenderer;

    public float speed = 5f;
    public float gravity = 5f;
    public float jumpforce = 10f;
    public float drag = 0.2f;
    public float accel = 0.65f;
    public float accel_air = 0.015f;

    private LineSegments _groundSeg;
    private Vector2 _internalPos;
    private Vector2 _lastFixedPos;
    private Vector2 _lastInternalPos;
    private float _distance;
    private Vector2 _velocity;
    public int _segIDX;

    private float _hInput;
    private float _lastHInput;
    private bool _jumpInput;
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
        _segIDX = 1;

        if (currentGround.lines.Count == 0)
            currentGround.CreateGround();
        _groundSeg = currentGround.lines;
        _internalPos = _groundSeg.segment[_segIDX].start;
        _lastInternalPos = _internalPos;
    }

    // Update is called once per frame
    void Update()
    {
        // 입력
        _hInput = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) + (Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f);
        //_vInput = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) + (Input.GetKey(KeyCode.DownArrow) ? -1f : 0f);

        _hInput += Mathf.Abs(Gamepad.current?.leftStick.x.value ?? 0.0f) > 0.4f ? Mathf.Sign(Gamepad.current?.leftStick.x.value ?? 0.0f) : 0f;
        //_vInput += Mathf.Abs(Gamepad.current?.leftStick.y.value ?? 0.0f) > 0.4f ? Mathf.Sign(Gamepad.current?.leftStick.y.value ?? 0.0f) : 0f;

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

        //목표 속력 -> 중력의 분력을 고려한 속도
        var groundDir = _groundSeg.segment[_segIDX].ToVector.normalized;
        var gravityForceVec = Vector2.Dot(Vector2.down * gravity, groundDir) * groundDir * 0.125f;
        var targetSpeed = speed + (Mathf.Abs(gravityForceVec.x) * (Mathf.Sign(_velocity.x) == Mathf.Sign(gravityForceVec.x) ? 1f : -0.55f));
        targetSpeed = Math.Clamp(targetSpeed, speed * 0.5f, speed * 1.5f);

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

        _distance += MathF.Sign(_velocity.x) * _velocity.magnitude * deltaTime;

        //세그먼트 이동
        //음수인 경우
        if(_distance < 0f)
        {
            while(_distance < 0f && _segIDX >= 0)
            {
                //탈출
                if(_segIDX == 0) // || {낭떨어지인 경우})
                {
                    _distance = 0f;
                    _velocity = Vector2.zero;
                    break;
                }

                //현재 라인이 벽인 경우
                var nextFrontNormal = _groundSeg.segment[_segIDX - 1].frontNormal;
                if (Mathf.Approximately(0.0f, nextFrontNormal.y) && nextFrontNormal.x > 0.0f)
                {
                    _distance = 0;
                    _velocity = Vector2.zero;
                    break;
                }

                _segIDX--;
                _distance += _groundSeg.segment[_segIDX].ToVector.magnitude;
            }
        }
        else if(_distance > _groundSeg.segment[_segIDX].ToVector.magnitude)
        {
            while(_distance > _groundSeg.segment[_segIDX].ToVector.magnitude && _segIDX <= _groundSeg.Count - 1)
            {
                //탈출
                if(_segIDX == _groundSeg.Count - 1)
                {
                    _distance = _groundSeg.segment[_segIDX].ToVector.magnitude;
                    _velocity = Vector2.zero;
                    break;
                }

                //현재 라인이 벽인 경우
                var nextFrontNormal = _groundSeg.segment[_segIDX + 1].frontNormal;
                if (Mathf.Approximately(0.0f, nextFrontNormal.y) && nextFrontNormal.x < 0.0f)
                {
                    _distance = _groundSeg.segment[_segIDX].ToVector.magnitude;
                    _velocity = Vector2.zero;
                    break;
                }

                _distance -= _groundSeg.segment[_segIDX].ToVector.magnitude;
                _segIDX++;
            }
        }

        _internalPos = _groundSeg.segment[_segIDX].CalcPosFromDistance(_distance);
        _lastHInput = _hInput;

        Debug.DrawRay(_internalPos + Vector2.up * 0.125f, _velocity, Color.red, 0.1f);
    }

    private void OnValidate()
    {
        if (!currentGround)
        {
            return;
        }

        if(_segIDX >= currentGround.lines.Count)
        {
            _segIDX = currentGround.lines.Count - 1;
        }
        else if(_segIDX < 0)
        {
            _segIDX = 0;
        }
    }
}

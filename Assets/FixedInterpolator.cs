using UnityEngine;

[DefaultExecutionOrder(-110)]
public class FixedInterpolator : MonoBehaviour
{
    public static FixedInterpolator instance;

    public float InterpolationFactor => _interpolationFactor;

    private float _interpolationFactor = 0;

    //private float[] _savedTimes = new float[2];
    //private int _checkIndex = 0;

    private float _lastFixedTime = 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            if (instance != null)
            {
                Destroy(gameObject);
            }
        }
    }

    public static void CreateInstance()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("Fixed Tick Interpolator");
            instance = go.AddComponent<FixedInterpolator>();
            DontDestroyOnLoad(go);
        }
    }

    private void OnDisable()
    {
        Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        //_checkIndex = OldTimeIndex();
        //_savedTimes[_checkIndex] = Time.fixedTime;

        _lastFixedTime = Time.time;
    }

    //public int OldTimeIndex()
    //{
    //    return _checkIndex == 0 ? 1 : 0;
    //}

    void Update()
    {
        //float newTime = _savedTimes[_checkIndex];
        //float oldTime = _savedTimes[OldTimeIndex()];

        //if (newTime != oldTime)
        //    _interpolationFactor = (Time.time - newTime) / (newTime - oldTime);
        //else
        //    _interpolationFactor = 1;

        _interpolationFactor = Mathf.Clamp01((Time.time - _lastFixedTime) / Time.fixedDeltaTime);
    }
}

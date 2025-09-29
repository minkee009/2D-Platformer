using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;

public class LineGround : MonoBehaviour
{
    public static Dictionary<int, LineGround> LineGroundMap = new Dictionary<int, LineGround>();

    public bool ShowDebugGroundBox = false;
    public bool ShowDebugGroundLine = true;

    [SerializeField]
    public List<Vector2> points = new List<Vector2>();

    [SerializeField]
    [HideInInspector]
    public LineSegments lines = new LineSegments();

    public IReadOnlyList<Line> walls => _walls;
    public IReadOnlyList<Line> footholds => _footholds;

    [SerializeField]
    [HideInInspector]
    public BBox extend;

    private LineRenderer _lineRenderer;
    private List<Line> _walls;
    private List<Line> _footholds;

    public IReadOnlyList<Line> CreateGround()
    {
        if (points == null || points.Count == 0)
            return null;

        float aabbMinX = float.PositiveInfinity;
        float aabbMinY = float.PositiveInfinity;
        float aabbMaxX = float.NegativeInfinity;
        float aabbMaxY = float.NegativeInfinity;

        lines.ResetPoint();
        foreach (var p in points)
        {
            aabbMinX = aabbMinX > p.x ? p.x : aabbMinX;
            aabbMinY = aabbMinY > p.y ? p.y : aabbMinY;
            aabbMaxX = aabbMaxX < p.x ? p.x : aabbMaxX;
            aabbMaxY = aabbMaxY < p.y ? p.y : aabbMaxY;

            lines.AddPoint(p);
        }

        _walls = new List<Line>();
        _footholds = new List<Line>();

        foreach (var line in lines.segment)
        {
            var frontNormal = line.frontNormal;

            if(frontNormal != Vector2.zero)
            {
                if (frontNormal.y > 0f)
                    _footholds.Add(line);
                else 
                    _walls.Add(line);
                continue;
            }
        }

        extend = new BBox(new Vector2(aabbMinX, aabbMinY), new Vector2(aabbMaxX, aabbMaxY));

        if (LineGroundMap.ContainsKey(GetInstanceID()))
            LineGroundMap.Remove(GetInstanceID());

        LineGroundMap.Add(GetInstanceID(), this);

        return lines.segment;
    }

    // Start is called before the first frame update
    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();

        if (_lineRenderer != null && points != null)
        {
            _lineRenderer.positionCount = points.Count;
            var idx = 0;
            foreach (var p in points)
            {
                _lineRenderer.SetPosition(idx++, new Vector3(p.x, p.y, 0f));
            }
            _lineRenderer.enabled = ShowDebugGroundLine;
        }

        CreateGround();
    }

    private void Update()
    {
        var lastShowDebugGroundLine = ShowDebugGroundLine;

        if (Input.GetKeyDown(KeyCode.F6))
        {
            ShowDebugGroundLine = !ShowDebugGroundLine;
        }

        if (lastShowDebugGroundLine != ShowDebugGroundLine)
            _lineRenderer.enabled = ShowDebugGroundLine;
    }

    private void OnDrawGizmos()
    {
        if (lines == null || lines.Count == 0)
            return;

        if (ShowDebugGroundBox && lines.segment != null && lines.segment.Count > 0)
        {
            DrawBBox(extend);

            foreach (var line in lines.segment)
            {
                var box = BBox.LineToBBox(line);
                DrawBBox(box);
            }
        }


        Gizmos.color = Color.red;
        if (ShowDebugGroundLine && lines.Count > 0)
        {
            foreach (var line in lines.segment)
            {
                Gizmos.DrawLine(line.start, line.end);
            }
        }
    }

    private void DrawBBox(BBox box)
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector2(box.min.x, box.min.y), new Vector2(box.max.x, box.min.y));
        Gizmos.DrawLine(new Vector2(box.max.x, box.min.y), new Vector2(box.max.x, box.max.y));
        Gizmos.DrawLine(new Vector2(box.max.x, box.max.y), new Vector2(box.min.x, box.max.y));
        Gizmos.DrawLine(new Vector2(box.min.x, box.max.y), new Vector2(box.min.x, box.min.y));
    }
}

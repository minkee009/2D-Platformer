using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LineGround : MonoBehaviour
{
    public bool ShowDebugGroundBox = false;
    public bool ShowDebugGroundLine = true;

    [SerializeField]
    public List<Vector2> points = new List<Vector2>();

    [SerializeField]
    [HideInInspector]
    public LineSegments lines = new LineSegments();

    [SerializeField]
    [HideInInspector]
    public BBox extend;

    private LineRenderer _lineRenderer;

    public IList<Line> CreateGround()
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

        extend = new BBox(new Vector2(aabbMinX, aabbMinY), new Vector2(aabbMaxX, aabbMaxY));

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

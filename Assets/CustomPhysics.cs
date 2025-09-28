using System;
using System.Collections.Generic;
using UnityEngine;

public struct CustomRayCastHit2D
{
    public Line hitLine;
    public Vector2 normal;
    public float distance;
    public Vector2 hitPoint;
}

public static class CustomPhysics
{
    public static bool Raycast2D(Vector2 position, Vector2 dir, float dist, Line line, out CustomRayCastHit2D hitInfo)
    {
        hitInfo = new CustomRayCastHit2D();

        Vector2 r = dir * dist;
        Vector2 s = line.ToVector;

        float rxs = Cross2D(r, s);
        Vector2 cma = line.start - position;

        float t = Cross2D(cma, s) / rxs;
        float u = Cross2D(cma, r) / rxs;

        if(t>= 0 && t <= 1
            && u >= 0 && u <= 1)
        {
            hitInfo.hitLine = line;
            hitInfo.distance = t * dist;
            hitInfo.hitPoint = new Vector2(position.x + t *r.x, position.y + t*r.y);
            hitInfo.normal = new Vector2(-s.y, s.x).normalized * Mathf.Sign(Vector2.Dot(hitInfo.normal, dir));
            return true;
        }

        return false;
    }
    public static bool CheckAABB(BBox a, BBox b)
    {
        return true;
    }
    

    public static float Cross2D(Vector2 a, Vector2 b)
    {
        return (a.x * b.y) - (a.y * b.x);
    }

    public static Vector3 Cross(Vector3 v1, Vector3 v2)
    {
        return new Vector3(
            v1.y * v2.z - v1.z * v2.y,
            v1.z * v2.x - v1.x * v2.z,
            v1.x * v2.y - v1.y * v2.x
            );
    }
}


[Serializable]
public struct Line
{
    public bool hasFrontNormal { get => frontNormal != Vector2.zero; }

    public Vector2 frontNormal;

    public Vector2 start, end;

    public Vector2 ToVector { get => end - start; }

    public Vector2 CalcPosFromDistance(float dist)
    {
        float max = ToVector.magnitude;
        return Vector2.Lerp(start, end, dist / max);
    }

    public Line(Vector2 start, Vector2 end)
    {
        this.start = start;
        this.end = end;
        frontNormal = Vector2.zero;
    }

    public Line(Vector2 start, Vector2 end, Vector2 frontNormal)
    {
        this.start = start;
        this.end = end;
        this.frontNormal = frontNormal;
    }

    public static Vector2 LineToVector(Line line)
    {
        return line.end - line.start;
    }
}

[Serializable]
public struct BBox
{
    public Vector2 min, max;

    public BBox(Vector2 min, Vector2 max)
    {
        this.min = min;
        this.max = max;
    }

    public static BBox LineToBBox(Line line)
    {
        var min = new Vector2(Math.Min(line.start.x, line.end.x), Math.Min(line.start.y, line.end.y));
        var max = new Vector2(Math.Max(line.start.x, line.end.x), Math.Max(line.start.y, line.end.y));
        return new BBox() { min = min, max = max };
    }
}

[Serializable]
public class LineSegments
{
    public IList<Line> segment;
    private List<Vector2> _points;
    private List<Line> _segment;

    public int Count => _segment.Count;

    public LineSegments()
    {
        _points = new List<Vector2>();
        _segment = new List<Line>();
        segment = _segment.AsReadOnly();
    }

    public void AddPoint(Vector2 point)
    {
        if (_points.Count > 0)
        {
            var line = new Line(_points[_points.Count - 1], point);

            Vector2 lineVector = line.ToVector;
            Vector2 potentialNormal = new Vector2(-lineVector.y, lineVector.x).normalized;

            line.frontNormal = potentialNormal;

            _segment.Add(line);
        }
        _points.Add(point);
    }

    public void AddLine(Line line)
    {
        _segment.Add(line);
    }

    public bool RemoveLastPoint()
    {
        if (_points.Count < 1)
            return false;

        if (_segment.Count > 0)
            _segment.RemoveAt(_points.Count - 1);

        _points.RemoveAt(_points.Count - 1);

        return true;
    }

    public bool RemovePointRange(int range)
    {
        if (range > _points.Count || _points.Count < 1)
            return false;

        while (range-- > 0)
        {
            if (_segment.Count > 0)
                _segment.RemoveAt(_segment.Count - 1);

            _points.RemoveAt(_points.Count - 1);
        }

        return true;
    }

    public void ResetPoint()
    {
        _points.Clear();
        _segment.Clear();
    }
}
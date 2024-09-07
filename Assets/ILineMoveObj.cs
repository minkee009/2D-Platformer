using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILineMoveObj
{
    public Vector2 ProjectPoint { get; }

    public Vector2 Velocity { get; set; }

    public void Move();
}

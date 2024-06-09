using System;
using UnityEngine;
using UnityEngine.UIElements;


public interface IPose
{
    public void Fill(IPose pose);

    public float GetRotation();

    public Vector2 GetPos();

    public IPose Aggregate(IPose other);

    public int GetHashCode();

    public bool Equals(object obj);
}

public class DefaultPose : IPose
{
    public Vector2 pos;
    public float rotation;

    public DefaultPose(float x, float y, float rotation)
    {
        this.pos = new Vector2(x, y);
        this.rotation = Mathf.Repeat(rotation, 2.0f * Mathf.PI);
    }

    public DefaultPose(Vector2 xy, float rotation)
    {
        this.pos = xy;
        this.rotation = Mathf.Repeat(rotation, 2.0f * Mathf.PI);
    }

    public IPose Aggregate(IPose other)
    {
        return new DefaultPose(pos.x + other.GetPos().x, pos.y + other.GetPos().y, rotation + other.GetRotation());
    }

    public void Fill(IPose pose)
    {
        this.pos = pose.GetPos();
        this.rotation = pose.GetRotation();
    }

    public float GetRotation() { return this.rotation; }

    public Vector2 GetPos() { return this.pos; }

    public override int GetHashCode()
    {
        return HashCode.Combine(pos.x, pos.y, rotation);
    }

    public override bool Equals(object obj)
    {
        if ((IPose)obj is IPose node)
        {
            return node.GetPos().Equals(pos) && node.GetRotation().Equals(rotation);
        }
        return false;
    }
}
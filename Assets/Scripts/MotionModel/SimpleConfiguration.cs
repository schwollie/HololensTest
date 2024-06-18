using System;
using UnityEngine;


public interface IConfiguration
{
    public void SetConfig(Vector3 config);
    public Vector3 Configuration();
    public float GetRotation();

    public Vector2 GetPos();

    public bool Equals(object obj)
    {
        if ((IConfiguration)obj is IConfiguration node)
        {
            return node.GetPos().Equals(GetPos()) && node.GetRotation().Equals(GetRotation());
        }
        return false;
    }

    public int GetHashCode()
    {
        return HashCode.Combine(GetPos().x, GetPos().y, GetRotation());
    }
}

public class SimpleConfiguration : IConfiguration
{
    public Vector3 config;

    public SimpleConfiguration(float x, float y, float rotation)
    {
        this.config = new Vector3(x, y, rotation);
    }

    public SimpleConfiguration(Vector2 xy, float rotation)
    {
        this.config = new Vector3(xy.x, xy.y, rotation);
    }

    public Vector3 Configuration() {  return config; }

    public void SetConfig(Vector3 config) {  this.config = config; }

    /*public IConfiguration Aggregate(IConfiguration other)
    {
        this.config = other.C
        return new DefaultPose(pos.x + other.GetPos().x, pos.y + other.GetPos().y, rotation + other.GetRotation());
    }

    public void Fill(IConfiguration pose)
    {
        this.pos = pose.GetPos();
        this.rotation = pose.GetRotation();
    }*/

    public float GetRotation() { return config[2]; }

    public Vector2 GetPos() { return new Vector2(config[0], config[1]); }
}
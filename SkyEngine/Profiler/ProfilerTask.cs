using System.Numerics;

namespace SkyEngine.Profiler;

public struct ProfilerTask
{
    private double startTime;
    private double endTime;
    private string name;
    private Vector3 color;
    
    public double Length => endTime - startTime;
}
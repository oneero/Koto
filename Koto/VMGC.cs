using System;
using UnityEngine;

public class VMGC : MonoBehaviour
{
    [SerializeField]
    private Robot robot;

    public void Start()
    {
        RobotData d = new RobotData(robot, Vector2.zero);
        robot.Init(d);
    }

    public void Wait()
    {
        return;
    }

    public void SendData(double portIndex, double data1, double data2)
    {
        robot.SendData(portIndex, data1, data2);
    }
}

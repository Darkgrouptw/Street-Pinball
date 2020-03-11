using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReplayClass
{
	public List<Vector3>	Position			= new List<Vector3>();
	public List<Vector3>	PinPosition			= new List<Vector3>();
	public List<float>		TimeTick			= new List<float>();
	public List<Vector2>	Velocity			= new List<Vector2>();
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReplayClass
{
	public string			Name				= "";
	public List<Vector3>	Position			= new List<Vector3>();
	public List<Vector3>	PinPosition			= new List<Vector3>();
	public List<float>		TimeTick			= new List<float>();
	public List<Vector2>	Velocity			= new List<Vector2>();
	public ParamsClass		Params				= new ParamsClass();
	public int				WinAreaNumber;
}

[System.Serializable]
public class TopCapsuleRecord
{
	public int HorizontalOffset = 0;
	public int VerticalOffset = 0;
}

[System.Serializable]
public class ParamsClass
{
	// 釘子位子組
	public List<TopCapsuleRecord> TopCapsuleList = new List<TopCapsuleRecord>();

	// 彈射群組
	public int				Power;
	public int				PushPower;
	public float			Angle;

	// 物理相關參數
	public float			TopcapsuleBounciness;
	public float			TopcapsuleFraction;
	public float			GroupBounciness;
	public float			GroupFraction;
	public float			BallBounciness;
	public float			BallFraction;
	public float			SeparateLineBounciness;
	public float			SeparateLineFraction;
	public float			Gravity;
	public float			HitPinBounciness;
	public float			HitPinFraction;
}
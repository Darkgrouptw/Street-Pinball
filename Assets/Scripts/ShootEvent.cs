using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShootEvent : MonoBehaviour
{
	[Header("===== Pinball =====")]
	public GameObject Ball;
	public GameObject Pin;

	[Header("===== Params =====")]
	public InputField PowerField;
	public InputField Angle0Field;
	public InputField Angle1Field;
	public InputField AutoTimesField;

	public void Shoot()
	{
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineTrigger : MonoBehaviour
{
	// 傳中獎區過去
	public ShootManager manager;

	private void OnTriggerEnter(Collider other)
	{
		manager.WinAreaNumber = int.Parse(this.name.Replace("Line", ""));
		Debug.Log(this.name);
	}
}

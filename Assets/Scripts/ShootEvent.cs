using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ShootAnimState
{
	NORMAL = 0,
	WAIT_FOR_SHOOT_ANIM,
	FREEZE_FOR_SHOOT_ANIM,
	SHOOT_ANIM,
	WAIT_BALL_ANIM,
}

public class ShootEvent : MonoBehaviour
{
	// 以上的
	// 力道 5 都是最小力道
	// 力道1 都是最大力道
	[Header("===== Pinball =====")]
	public GameObject		Ball;																	// 球
	public GameObject		Pin;																	// 上方那根針的位置
	public Vector3			PowerSixLocation = new Vector3(-0.63f, -0.8930f, 0);					// 不使用時的位置
	public Vector3			PowerOneLocation = new Vector3(-0.63f, -1.2530f, 0);					// 最大力道時的位置
	public ShootAnimState	state = ShootAnimState.NORMAL;											// 狀態
	public float			AddForce5 = 2000;														// 最小力道時的 Force
	public float			AddForce1 = 5000;														// 最大力道時的 Force

	[Header("===== Params =====")]
	public InputField		PowerField;
	public InputField		Angle0Field;
	public InputField		Angle1Field;
	public InputField		AutoTimesField;

	[Header("===== 演出相關 =====")]
	private int				power = 5;
	private float			angle = 0;
	private int				autoTime = 1;
	private float			currentTime = 0;
	private Vector3			BallFirstPos = Vector3.zero;
	private Vector3			PinFirstPos = Vector3.zero;
	private List<GameObject> BallList = new List<GameObject>();
	public float			WaitForShoot_Time = 1.5f;
	public float			Freeze_Shoot_Time = 0.5f;
	public float			Shoot_Time = 0.5f;
	public float			WaitBall_Time = 3;
	private float			TimeCounter = 0;

	private void FixedUpdate()
	{
		// 增加時間
		TimeCounter += Time.deltaTime;
		if (TimeCounter >= 10000)
			TimeCounter = 0;		// 不要讓他抱調

		// 跑每個 State 的動畫
		switch (state)
		{
			case ShootAnimState.WAIT_FOR_SHOOT_ANIM:
				{
					// 拿力道
					float powerRatio = 1 - (float)power / 6;
					float t = Mathf.Lerp(0, powerRatio, TimeCounter / WaitForShoot_Time);
					Pin.transform.position = Vector3.Lerp(PowerSixLocation, PowerOneLocation, t);

					// 判斷是否超過
					if (TimeCounter >= WaitForShoot_Time)
					{
						TimeCounter = 0;
						state = ShootAnimState.FREEZE_FOR_SHOOT_ANIM;
					}
					break;
				}
			case ShootAnimState.FREEZE_FOR_SHOOT_ANIM:
				{
					// 判斷是否超過
					if (TimeCounter >= Freeze_Shoot_Time)
					{
						TimeCounter = 0;
						state = ShootAnimState.SHOOT_ANIM;

						// 加力量
						float t = 1-  power / 5;
						float force = Mathf.Lerp(AddForce5, AddForce1, t);
						float sinForce = Mathf.Sin(angle * Mathf.Deg2Rad) * force;
						float cosForce = Mathf.Cos(angle * Mathf.Deg2Rad) * force;
						BallList[BallList.Count - 1].GetComponent<Rigidbody>().AddForce(new Vector3(sinForce, cosForce, 0));
					}
					break;
				}
			case ShootAnimState.SHOOT_ANIM:
				{
					float t = Mathf.Clamp01(TimeCounter /= Shoot_Time);
					Pin.transform.position = Vector3.Lerp(Pin.transform.position, PinFirstPos, t);

					// 判斷是否超過
					if (TimeCounter >= Shoot_Time)
					{
						TimeCounter = 0;
						Pin.transform.position = PinFirstPos;
						state = ShootAnimState.WAIT_BALL_ANIM;
					}
					break;
				}
			case ShootAnimState.WAIT_BALL_ANIM:
				{
					// 判斷是否超過
					if (TimeCounter >= WaitBall_Time)
					{
						TimeCounter = 0;

						// 重製一顆球
						currentTime++;
						if (currentTime == autoTime)
						{
							for (int i = BallList.Count - 1; i >= 1; i--)
							{
								GameObject temp = BallList[i];
								Destroy(temp);
								BallList.RemoveAt(i);
							}
							BallList[0].transform.position = BallFirstPos;

							// 結束了
							state = ShootAnimState.NORMAL;
						}
						else
						{
							GameObject temp = GameObject.Instantiate(BallList[0]);
							temp.transform.position = BallFirstPos;
							BallList.Add(temp);

							// 繼續
							state = ShootAnimState.WAIT_FOR_SHOOT_ANIM;
						}
					}
					break;
				}
		}
	}

	public void Shoot()
	{
		if(state == ShootAnimState.NORMAL)
		{
			TimeCounter = 0;
			currentTime = 0;
			state = ShootAnimState.WAIT_FOR_SHOOT_ANIM;

			// 拿值
			power = int.Parse(PowerField.text);
			float temp1 = float.Parse(Angle0Field.text);
			float temp2 = float.Parse(Angle1Field.text);
			angle = Random.Range(temp1, temp2);
			autoTime = int.Parse(AutoTimesField.text);

			// 初始給值
			if (BallFirstPos == Vector3.zero)
			{
				BallFirstPos = Ball.transform.position;
				PinFirstPos = Pin.transform.position;
				BallList.Add(Ball);
			}
		}
		//Debug.Log(Pin.transform.position.ToString("F4"));
	}

}

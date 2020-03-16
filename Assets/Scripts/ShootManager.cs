using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;

public enum ShootAnimState
{
	// 正常丟球模式
	NORMAL = 0,
	WAIT_FOR_SHOOT_ANIM,
	FREEZE_FOR_SHOOT_ANIM,
	SHOOT_ANIM,
	WAIT_BALL_ANIM,

	// 重播
	REPLAY_ANIM,
}

public class TopCapsuleInfo
{
	public GameObject capsule;
	public int HorizontalOffset = 0;
	public int VerticalOffset = 0;

	public int NameID = 0;
}

[RequireComponent(typeof(ReplayManager))]
public class ShootManager : MonoBehaviour
{
	// 以上的
	// 力道 5 都是最小力道
	// 力道1 都是最大力道
	[Header("===== Pinball =====")]
	public GameObject Ball;                                                             // 球
	public GameObject Pin;																// 下方推桿的位置
	public GameObject TopCapsule;                                                       // 最上方真的位置
	public Vector3 PowerSixLocation = new Vector3(-0.63f, -0.8930f, 0);                 // 不使用時的位置
	public Vector3 PowerOneLocation = new Vector3(-0.63f, -1.2530f, 0);                 // 最大力道時的位置
	public Vector2 TopCapsule_MinMaxHorizontal = new Vector2(-0.2690f, 0.2690f);        // 上方的水平位置 (最小、最大)
	public Vector2 TopCapsule_MinMaxVertical = new Vector2(0.3180f, 0.5630f);           // 上方的垂直位置 (最小、最大)
	public ShootAnimState state = ShootAnimState.NORMAL;                                // 狀態
	public float AddForce5 = 2000;                                                      // 最小力道時的 Force
	public float AddForce1 = 5000;                                                      // 最大力道時的 Force

	[Header("===== Params =====")]
	public Dropdown TopCapsuleDD;
	public InputField PowerField;
	public InputField Angle0Field;
	public InputField Angle1Field;
	public InputField AutoTimesField;
	public InputField HorizontalField;
	public InputField VerticalField;
	public InputField TopcapsuleBouncinessField;
	public InputField TopcapsuleFractionField;
	public InputField GroupBouncinessField;
	public InputField GroupFractionField;
	public InputField BallBouncinessField;
	public InputField BallFractionField;
	public InputField SeparateLineBouncinessField;
	public InputField SeparateLineFractionField;
	public InputField GravityField;
	public InputField HitPinBouncinessField;
	public InputField HitPinFractionField;

	[Header("===== Physic Material =====")]
	public PhysicMaterial TopcapsulePM;
	public PhysicMaterial GroupPM;
	public PhysicMaterial BallPM;
	public PhysicMaterial SeparateLinePM;
	public PhysicMaterial HitPinPM;

	[Header("===== 演出相關 =====")]
	private int power = 5;
	private float angle = 0;
	private int autoTime = 1;                                                           // 總共重播要幾次
	private float currentTime = 0;                                                      // 目前重播是第幾次
	private int ReplayIndex = 0;                                                        // 重播哪一個檔案
	private int SelectCapsuleIndex = 0;													// 目前選到的是哪一個 Capsule
	private Vector3 BallFirstPos = Vector3.zero;
	private Vector3 PinFirstPos = Vector3.zero;
	private List<GameObject> BallList = new List<GameObject>();							// 球的 List
	private List<TopCapsuleInfo> TopCapsuleList = new List<TopCapsuleInfo>();			// 針的數量
	public float WaitForShoot_Time = 1.5f;
	public float Freeze_Shoot_Time = 0.5f;
	public float Shoot_Time = 0.5f;
	public float WaitBall_Time = 1;
	private Vector3 LastBallPos;
	private float TimeCounter = 0;														// 判斷狀態轉換的計數器
	private float WorldTimeCounter = 0;													// 重播紀錄，與使用的計數器
	private float LastTimeCounter = 0;                                                  // 維持停止的計數器
	private int LastSearchIndex = 0;													// 前 Index 的 Frame 總共多少時間
	private ReplayManager ReplayM;
	private List<ReplayClass> ReplayInfo = new List<ReplayClass>();                     // 重播
	private const int MinMass = 5;														// 重力參數
	private const int MaxMass = 15;														// 重力參數

	private void Start()
	{
		// 初始給值
		BallFirstPos = Ball.transform.position;
		PinFirstPos = Pin.transform.position;
		BallList.Add(Ball);
		ReplayM = this.GetComponent<ReplayManager>();

		// 上方設定
		TopCapsule.SetActive(false);
		int horizontalOffset = int.Parse(HorizontalField.text);
		int verticalOffset = int.Parse(VerticalField.text);
		TopCapsuleInfo tempInfo = new TopCapsuleInfo();
		tempInfo.capsule = GameObject.Instantiate<GameObject>(TopCapsule);
		tempInfo.HorizontalOffset = horizontalOffset;
		tempInfo.VerticalOffset = verticalOffset;
		tempInfo.capsule.SetActive(true);
		tempInfo.capsule.transform.SetParent(TopCapsule.GetComponentInParent<Transform>());
		tempInfo.NameID = 0;
		TopCapsuleList.Add(tempInfo);

		MoveTopCapsule();
		ResetPhysicMaterial();
	}

	private void Update()
	{
		switch (state)
		{
			// 重播
			case ShootAnimState.REPLAY_ANIM:
				{
					WorldTimeCounter += Time.deltaTime;

					bool IsFind = false;
					for(int i = LastSearchIndex; i < ReplayInfo[ReplayIndex].TimeTick.Count - 1; i++)
					{
						float CurrentTimeTick		= ReplayInfo[ReplayIndex].TimeTick[i + 0];
						float NextTimeTick			= ReplayInfo[ReplayIndex].TimeTick[i + 1];
						if (CurrentTimeTick <= WorldTimeCounter && WorldTimeCounter <= NextTimeTick)
						{
							IsFind = true;
							Vector3 CurrentBallPos	= ReplayInfo[ReplayIndex].Position[i + 0];
							Vector3 NextBallPos		= ReplayInfo[ReplayIndex].Position[i + 1];
							Vector3 CurrentPinPos	= ReplayInfo[ReplayIndex].PinPosition[i + 0];
							Vector3 NextPinPos		= ReplayInfo[ReplayIndex].PinPosition[i + 1];
							float t = Mathf.Clamp01((WorldTimeCounter - CurrentTimeTick) / Mathf.Max(NextTimeTick - CurrentTimeTick, 0.0001f));

							// 設定位置
							BallList[0].transform.position	= Vector3.Lerp(CurrentBallPos, NextBallPos, t);
							Pin.transform.position			= Vector3.Lerp(CurrentPinPos, NextPinPos, t);
							LastSearchIndex = i;
							break;
						}
					}
					if (!IsFind)
					{
						TimeCounter = 0;
						WorldTimeCounter = 0;
						ReplayIndex = -1;


						BallList[0].transform.position = BallFirstPos;
						BallList[0].GetComponent<Rigidbody>().useGravity = true;

						// 結束了
						state = ShootAnimState.NORMAL;
					}
					break;
				}
		}
	}

	private void FixedUpdate()
	{
		// 增加時間
		TimeCounter += Time.fixedDeltaTime;
		if (TimeCounter >= 10000)
			TimeCounter = 0;        // 不要讓他抱調

		// 只要他還在跑的時候
		if (state >= ShootAnimState.WAIT_FOR_SHOOT_ANIM && state <= ShootAnimState.WAIT_BALL_ANIM)
			WorldTimeCounter += Time.fixedDeltaTime;

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
					RecordCurrentBallData();
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
						float t = 1 - power / 5;
						float force = Mathf.Lerp(AddForce5, AddForce1, t);
						float sinForce = Mathf.Sin(angle * Mathf.Deg2Rad) * force;
						float cosForce = Mathf.Cos(angle * Mathf.Deg2Rad) * force;
						BallList[BallList.Count - 1].GetComponent<Rigidbody>().AddForce(new Vector3(sinForce, cosForce, 0));
					}
					RecordCurrentBallData();
					break;
				}
			case ShootAnimState.SHOOT_ANIM:
				{
					float t = Mathf.Clamp01(TimeCounter /= Shoot_Time);
					Pin.transform.position = Vector3.Lerp(Pin.transform.position, PinFirstPos, t * t * t);

					// 判斷是否超過
					if (TimeCounter >= Shoot_Time)
					{
						TimeCounter = 0;
						Pin.transform.position = PinFirstPos;
						state = ShootAnimState.WAIT_BALL_ANIM;
					}
					RecordCurrentBallData();
					break;
				}
			case ShootAnimState.WAIT_BALL_ANIM:
				{
					// 判斷是否超過
					if (LastTimeCounter >= WaitBall_Time)
					{
						TimeCounter = 0;
						WorldTimeCounter = 0;
						LastTimeCounter = 0;
						LastBallPos = BallFirstPos;

						// 重製一顆球
						currentTime++;
						BallList[BallList.Count - 1].GetComponent<Rigidbody>().velocity = Vector3.zero;
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

							// 加重播清單
							ReplayInfo.Add(new ReplayClass());

							// 繼續
							state = ShootAnimState.WAIT_FOR_SHOOT_ANIM;
						}

						// 重播選單更新
						ReplayM.GenerateUIItem();
					}
					else if (Vector3.Distance(LastBallPos, BallList[BallList.Count - 1].transform.position) <= 0.1f)
						// 加時間
						LastTimeCounter += Time.deltaTime;
					else
					{
						LastTimeCounter = 0;
						LastBallPos = BallList[BallList.Count - 1].transform.position;
					}
					RecordCurrentBallData();
					break;
				}
		}
	}
	// 外部呼叫函示
	public void Shoot()
	{
		if (state == ShootAnimState.NORMAL)
		{
			TimeCounter = 0;
			WorldTimeCounter = 0;
			currentTime = 0;
			state = ShootAnimState.WAIT_FOR_SHOOT_ANIM;
			

			// 拿值
			power = int.Parse(PowerField.text);
			float temp1 = float.Parse(Angle0Field.text);
			float temp2 = float.Parse(Angle1Field.text);
			angle = Random.Range(temp1, temp2);
			autoTime = int.Parse(AutoTimesField.text);

			ReplayInfo.Add(new ReplayClass());
			RecordCurrentBallData();
		}
		//Debug.Log(TopCapsule.transform.position.ToString("F4"));
		//Debug.Log(Pin.transform.position.ToString("F4"));
	}


	// Top Capsule 相關
	public void AddTopCapsule()
	{
		int lastText = TopCapsuleList[TopCapsuleList.Count - 1].NameID;
		TopCapsuleDD.options.Add(new Dropdown.OptionData(text: (lastText + 1).ToString()));

		TopCapsuleInfo tempInfo = new TopCapsuleInfo();
		tempInfo.capsule = GameObject.Instantiate<GameObject>(TopCapsule);
		tempInfo.HorizontalOffset = 0;
		tempInfo.VerticalOffset = 0;
		tempInfo.capsule.SetActive(true);
		tempInfo.capsule.transform.SetParent(TopCapsule.GetComponentInParent<Transform>());
		tempInfo.NameID = lastText + 1;
		TopCapsuleList.Add(tempInfo);

		// 改變 List
		HorizontalField.text = "0";
		VerticalField.text = "0";
		SelectCapsuleIndex = TopCapsuleList.Count - 1;
		TopCapsuleDD.value = SelectCapsuleIndex;

		MoveTopCapsule();
	}
	public void DeleteTopCapsule()
	{
		if (TopCapsuleList.Count > 1)
		{
			GameObject.Destroy(TopCapsuleList[SelectCapsuleIndex].capsule);

			TopCapsuleList.RemoveAt(SelectCapsuleIndex);
			TopCapsuleDD.options.RemoveAt(SelectCapsuleIndex);

			if (TopCapsuleList.Count == SelectCapsuleIndex)
				SelectCapsuleIndex--;

			TopCapsuleDD.captionText.text = TopCapsuleDD.options[SelectCapsuleIndex].text;

			HorizontalField.text = TopCapsuleList[SelectCapsuleIndex].HorizontalOffset.ToString();
			VerticalField.text = TopCapsuleList[SelectCapsuleIndex].VerticalOffset.ToString();
		}
	}
	public void ChangeIndexTopCapsule()
	{
		SelectCapsuleIndex = TopCapsuleDD.value;
		if (TopCapsuleList.Count > SelectCapsuleIndex)
		{
			TopCapsuleInfo info = TopCapsuleList[SelectCapsuleIndex];

			HorizontalField.text = info.HorizontalOffset.ToString();
			VerticalField.text = info.VerticalOffset.ToString();

		}
	}
	public void MoveTopCapsule()
	{
		if (TopCapsuleList.Count > SelectCapsuleIndex)
		{
			TopCapsuleInfo info = TopCapsuleList[SelectCapsuleIndex];

			int horizontalOffset = int.Parse(HorizontalField.text);
			int verticalOffset = int.Parse(VerticalField.text);
			info.HorizontalOffset = horizontalOffset;
			info.VerticalOffset = verticalOffset;

			float x = Mathf.Lerp(TopCapsule_MinMaxHorizontal.x, TopCapsule_MinMaxHorizontal.y, (float)(horizontalOffset + 20) / 40);	// 來回 -20 ~ 20 之間
			float y = Mathf.Lerp(TopCapsule_MinMaxVertical.x, TopCapsule_MinMaxVertical.y, 1 - (float)(verticalOffset) / 30);			// 來回 0 ~ 30 之間
			info.capsule.transform.position = new Vector3(x, y, info.capsule.transform.position.z);
		}
	}
	
	// 設定彈性相關係數
	public void ResetPhysicMaterial()
	{
		#region 變數宣告
		float BouncinessLevel = 0;
		float FractionLevel = 0;

		const int MAX_LEVEL = 5;
		#endregion
		#region 釘子
		BouncinessLevel				= Mathf.Clamp01(float.Parse(TopcapsuleBouncinessField.text) / MAX_LEVEL);
		FractionLevel				= Mathf.Clamp01(float.Parse(TopcapsuleFractionField.text) / MAX_LEVEL);

		TopcapsulePM.dynamicFriction= FractionLevel;
		TopcapsulePM.staticFriction	= FractionLevel;
		TopcapsulePM.bounciness		= BouncinessLevel;
		#endregion
		#region 框架
		BouncinessLevel				= Mathf.Clamp01(float.Parse(GroupBouncinessField.text) / MAX_LEVEL);
		FractionLevel				= Mathf.Clamp01(float.Parse(GroupFractionField.text) / MAX_LEVEL);

		GroupPM.dynamicFriction		= FractionLevel;
		GroupPM.staticFriction		= FractionLevel;
		GroupPM.bounciness			= BouncinessLevel;
		#endregion
		#region 球
		BouncinessLevel				= Mathf.Clamp01(float.Parse(BallBouncinessField.text) / MAX_LEVEL);
		FractionLevel				= Mathf.Clamp01(float.Parse(BallFractionField.text) / MAX_LEVEL);

		BallPM.dynamicFriction		= FractionLevel;
		BallPM.staticFriction		= FractionLevel;
		BallPM.bounciness			= BouncinessLevel;
		#endregion
		#region 分割線
		BouncinessLevel				= Mathf.Clamp01(float.Parse(SeparateLineBouncinessField.text) / MAX_LEVEL);
		FractionLevel				= Mathf.Clamp01(float.Parse(SeparateLineFractionField.text) / MAX_LEVEL);

		SeparateLinePM.dynamicFriction= FractionLevel;
		SeparateLinePM.staticFriction= FractionLevel;
		SeparateLinePM.bounciness	= BouncinessLevel;
		#endregion
		#region 重力
		int gravityLevel = int.Parse(GravityField.text);

		gravityLevel = Mathf.Clamp(gravityLevel, 1, 5);
		Ball.GetComponent<Rigidbody>().mass = (MaxMass - MinMass) * gravityLevel / 4 + MinMass;
		#endregion
		#region 撞針
		BouncinessLevel				= Mathf.Clamp01(float.Parse(HitPinBouncinessField.text) / MAX_LEVEL);
		FractionLevel				= Mathf.Clamp01(float.Parse(HitPinFractionField .text) / MAX_LEVEL);

		HitPinPM.dynamicFriction	= FractionLevel;
		HitPinPM.staticFriction		= FractionLevel;
		HitPinPM.bounciness			= BouncinessLevel;
		#endregion
	}

	// 輸出相關的東西
	public void ExportOneFile()
	{
		int index = ReplayM.FocusIndex;
		string json = JsonUtility.ToJson(ReplayInfo[index]);

		if (!System.IO.Directory.Exists("Results"))
			System.IO.Directory.CreateDirectory("Results");
		string location = "Results/n" + index + ".txt";
		System.IO.File.WriteAllText(location, json);
		Debug.Log("輸出成功: " + location);
	}
	public void ExportAllFile()
	{
		if (!System.IO.Directory.Exists("Results"))
			System.IO.Directory.CreateDirectory("Results");
		for (int i = 0; i < ReplayInfo.Count; i++)
		{
			string json = JsonUtility.ToJson(ReplayInfo[i]);

			string location = "Results/n" + i + ".txt";
			System.IO.File.WriteAllText(location, json);
		}
		Debug.Log("全部輸出成功，共" + ReplayInfo.Count + "個");
	}
	public void ImportFile()
	{
		string initLocation = "./Results";
#if UNITY_EDITOR
		initLocation = "./Builds/Results";
#endif
		FileBrowser.ShowLoadDialog(
			delegate (string path)
			{
				string txt = System.IO.File.ReadAllText(path);
				ReplayClass temp = JsonUtility.FromJson<ReplayClass>(txt);

				ReplayInfo.Add(temp);
				ReplayM.GenerateUIItem();
			},                                                                                  // 按下成功時 
			delegate () { Debug.Log("Canceled"); },                                             // 取消
			false,                                                                              // 是否選擇資料夾
			initLocation,																		// 初始位置
			"選擇檔案",                                                                         // Title 顯示名稱
			"選擇"                                                                              // submit 按鈕名稱
		);
	}
	public void Replay(int index)
	{
		if (state == ShootAnimState.NORMAL)
		{
			TimeCounter = 0;
			WorldTimeCounter = 0;
			LastSearchIndex = 0;
			state = ShootAnimState.REPLAY_ANIM;

			ReplayIndex = index;

			BallList[0].GetComponent<Rigidbody>().useGravity = false;
			BallList[0].transform.position = ReplayInfo[ReplayIndex].Position[0];
			Pin.transform.position = ReplayInfo[ReplayIndex].PinPosition[0];
		}
	}
	public void ResetUI()
	{
		HorizontalField.text = (0).ToString();
		VerticalField.text = (0).ToString();
		PowerField.text = (5).ToString();
		Angle0Field.text = (-2).ToString();
		Angle1Field.text = (2).ToString();
		AutoTimesField.text = (1).ToString();
	}


	// 紀錄
	private void RecordCurrentBallData()
	{
		var LastElement = ReplayInfo[ReplayInfo.Count - 1];
		LastElement.Position.Add(BallList[BallList.Count - 1].transform.position);
		LastElement.PinPosition.Add(Pin.transform.position);
		LastElement.TimeTick.Add(WorldTimeCounter);
		LastElement.Velocity.Add(BallList[BallList.Count - 1].GetComponent<Rigidbody>().velocity);
	}
}
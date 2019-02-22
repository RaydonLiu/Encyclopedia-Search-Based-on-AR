using System.Collections;
using System.Collections.Generic;
using UnityEngine; 

public class ActionListen : MonoBehaviour {

	private PersonButtonListen myPersonButtonListen;  //个人中心按钮
	private PersonalPanel myPersonalPanel;  		  //个人中心面板
	private SetDialog mySetDialog;					  //设置按钮
	private Animation  buttonAnimation; 			  //个人中心按钮动画
	private Animation  setButtonAnimation; 			  //设置按钮动画

	private float fingerActionSensitivity = Screen.width * 0.05f; //手指动作的敏感度，这里设定为 二十分之一的屏幕宽度.
	//
	private float fingerBeginX;
	private float fingerBeginY;
	private float fingerCurrentX;
	private float fingerCurrentY;
	private float fingerSegmentX;
	private float fingerSegmentY;
	//
	private int fingerTouchState;
	//
	private int FINGER_STATE_NULL = 0;
	private int FINGER_STATE_TOUCH = 1;
	private int FINGER_STATE_ADD = 2;
	// Use this for initialization
	void Start () 
	{
		fingerActionSensitivity = Screen.width * 0.05f;

		fingerBeginX = 0;
		fingerBeginY = 0;
		fingerCurrentX = 0;
		fingerCurrentY = 0;
		fingerSegmentX = 0;
		fingerSegmentY = 0;

		fingerTouchState = FINGER_STATE_NULL;
		
		myPersonButtonListen = FindObjectOfType<PersonButtonListen>();
		myPersonalPanel  = FindObjectOfType<PersonalPanel>();
		mySetDialog = FindObjectOfType<SetDialog>();
		buttonAnimation = myPersonButtonListen.GetComponent<Animation>();
		setButtonAnimation = mySetDialog.GetComponent<Animation>();
		
		if(myPersonButtonListen){
			myPersonButtonListen.gameObject.SetActive(false);
		}
		if(myPersonalPanel){
			myPersonButtonListen.gameObject.SetActive(false);
		}
		if(mySetDialog){
			mySetDialog.gameObject.SetActive(false);
		}
	}
	
	// Update is called once per frame
	void Update ()
	{

		if (Input.GetKeyDown (KeyCode.Mouse0)) 
		{
			if(fingerTouchState == FINGER_STATE_NULL)
			{
			fingerTouchState = FINGER_STATE_TOUCH;
			fingerBeginX = Input.mousePosition.x;
			fingerBeginY = Input.mousePosition.y;
			}

		}

		if(fingerTouchState == FINGER_STATE_TOUCH)
		{
			fingerCurrentX = Input.mousePosition.x;
			fingerCurrentY = Input.mousePosition.y;
			fingerSegmentX = fingerCurrentX - fingerBeginX;
			fingerSegmentY = fingerCurrentY - fingerBeginY;
		}


		if (fingerTouchState == FINGER_STATE_TOUCH) 
		{
			float fingerDistance = fingerSegmentX*fingerSegmentX + fingerSegmentY*fingerSegmentY; 

			if (fingerDistance > (fingerActionSensitivity*fingerActionSensitivity))
			{
			toAddFingerAction();
			}
		}

		if (Input.GetKeyUp(KeyCode.Mouse0))
		{
			fingerTouchState = FINGER_STATE_NULL;
		}
	}

	private void toAddFingerAction()
	{

		fingerTouchState = FINGER_STATE_ADD;

		if (Mathf.Abs (fingerSegmentX) > Mathf.Abs (fingerSegmentY)){
			fingerSegmentY = 0;
		} 
		else{
		fingerSegmentX = 0;
		}

		if (fingerSegmentX == 0) {
			if (fingerSegmentY > 0){
				Debug.Log ("up");  //向上滑动  倒放动画
				if(myPersonButtonListen){
					buttonAnimation["fadeIn"].time = buttonAnimation["fadeIn"].clip.length;
					buttonAnimation["fadeIn"].speed = -1;
					buttonAnimation.Play();
					
					setButtonAnimation["fadeIn2"].time = setButtonAnimation["fadeIn2"].clip.length;
					setButtonAnimation["fadeIn2"].speed = -1;
					setButtonAnimation.Play();
				}
				if(myPersonalPanel){
					myPersonButtonListen.gameObject.SetActive(false);
				}	
			} else {
				Debug.Log ("down");  //向下滑动  正序播放
				if(myPersonButtonListen){
					myPersonButtonListen.gameObject.SetActive(true);
					mySetDialog.gameObject.SetActive(true);
					buttonAnimation["fadeIn"].speed = 1;
					setButtonAnimation["fadeIn2"].speed = 1;
					buttonAnimation.Play();		
					setButtonAnimation.Play();
				}
			}
		} 
		else if(fingerSegmentY == 0) {
			if(fingerSegmentX > 0){
				Debug.Log ("right");
							
			}else{
				Debug.Log("left");
				
			}
		}

		}
}

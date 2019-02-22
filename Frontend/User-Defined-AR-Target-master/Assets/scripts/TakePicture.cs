using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.UI;
using LitJson; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;
   


public class TakePicture : MonoBehaviour {

	private GameObject title; 		 //显示的title = 类名
	private GameObject info;  		 //显示的内容 = 信息
	private GameObject Cube;  		 //显示的面板

	public UDTEventHandler udteventhandler;  //调用外部类
	
	public byte[] imageByteArray;    //图片的二进制
	
	private string url = "http://192.168.88.33:5000/api/upload";    //服务器地址
	
	private string imageURl;         //图片地址
	
	public class MessageJson  
	{  
		public string className;     //类名
		public string information;   //信息
		public float locationX;      //坐标x
		public float locationY;		 //左边y
		public float wide;   		 //宽度
		public float height;		 //高度
		public string score;  		 //分数
	}  
	
	public MessageJson myMessage = new MessageJson();   //解析的json
	
	public bool canTakePhotos ;    		 //标记是否服务器返回了数据，开始拍下一张照片
	private MyDialog mMyDialog;          		 //无法连接服务器提示信息面板
	private QualityDialog mQualityDialog;        //无法识别提示信息面板     
	private PersonalPanel myPersonalPanel;  
	
	// 开始
	void Start(){
		//获取Text获取Cube、title、info
		title = GameObject.Find("title");
		info = GameObject.Find("information");
		Cube = GameObject.Find("Cube");
		canTakePhotos = true;  
		mMyDialog = FindObjectOfType<MyDialog>(); 
		mQualityDialog = FindObjectOfType<QualityDialog>();
		myPersonalPanel = FindObjectOfType<PersonalPanel>();
		
        if (myPersonalPanel){
            myPersonalPanel.gameObject.SetActive(false); 
		
        }
		
        if (mMyDialog){
            mMyDialog.gameObject.SetActive(false); //设置提示面板的不显示
		
        }
		
        if (mQualityDialog){
            mQualityDialog.gameObject.SetActive(false);//设置提示面板的不显示
		
        }
		
		StartCoroutine ("StartCamera");
	}

	
	//打开照相机
	public void StartCamera(){
		//StartCoroutine ("AutoTakePhotos");
		InvokeRepeating("AutoTakePhotos", 1, 1.5F);  //1秒后，每1f调用一次
	}
	
	//定时自动拍照
	public void AutoTakePhotos() {
		if(canTakePhotos){
			//canTakePhotos = false;  //关闭自动拍照，等待服务器返回数据后才开始
			StartCoroutine ("TakePhoto");	
		}else{
			print("can't take photos");
			print(canTakePhotos);
		}
    }
	
	
	//开始拍照
	public IEnumerator TakePhoto(){
		string filePath;
		
			//在手机上的图片位置
			if (Application.isMobilePlatform) {
				filePath = Application.persistentDataPath + "/image.png";
				ScreenCapture.CaptureScreenshot ("/image.png");
				yield return new WaitForSeconds(1.5f);
				//把图片转为二进制
				imageByteArray = File.ReadAllBytes(filePath);
				
			} else {
				//电脑上的图片位置
				filePath = Application.dataPath + "/StreamingAssets/" + "image.png";
				ScreenCapture.CaptureScreenshot (filePath);
				yield return new WaitForSeconds(1.5f);
				imageByteArray = File.ReadAllBytes(filePath);
			}
			
			Debug.Log("byte:"+imageByteArray);
			
			print("photo done!!");
			StartCoroutine("UploadImages");
			
			
		Debug.Log("byte:"+imageByteArray);
	
	}
	
	
	public IEnumerator UploadImages(){
		
		print ("uploading image...");
		WWWForm myForm = new WWWForm ();
		//设置表单头
		myForm.AddBinaryData("myfile", imageByteArray, "image.png"); 
		//post请求
		WWW www = new WWW(url,myForm);
		
		//等待完成
		yield return www;
		
		print ("done uploading!");

		  if (www.error != null)
        {
            Debug.Log("Error:"+www.error);
			//显示提示 无法连接服务器
			if (mMyDialog)
			{
				mMyDialog.gameObject.SetActive(true);
			}
            yield return null;
        }
        else
        {
			
			canTakePhotos = true; //设置可以开始下一次的自动拍照
            if(mMyDialog)
			{
				mMyDialog.gameObject.SetActive(false);
			}
			

		    print ("uploadImage successful!");
			string a = www.text;
			int num = a.Length;
			//Debug.Log("DATA:"+www.text);
			//print(num);
			
			if(num > 3){	
				if (mQualityDialog){
					mQualityDialog.gameObject.SetActive(false);
				}
			
				//处理服务器上传输过来的json
				JsonData[] originalJson = JsonMapper.ToObject<JsonData[]>(a);
	
				myMessage.className = originalJson[0]["className"].ToString();
				
				//处理文字
				myMessage.score = originalJson[0]["score"].ToString();
				myMessage.information = originalJson[0]["information"].ToString();
				int length = myMessage.information.Length;
				string info = myMessage.information;
				for(int i= 10; i<length;i++){
					if(i%10 == 0){
						info = info.Insert(i,"\n");
						//length = length+2;
					}
				}
				myMessage.information=info;
				print(myMessage.information);
				
				//处理location
				string location = originalJson[0]["location"].ToString();
				location = location.Replace("[","").Replace("]","").Replace("'","");
				string[] locations=location.Split(',');
				myMessage.locationX = float.Parse(locations[0]);
				myMessage.locationY = float.Parse(locations[1]);
				myMessage.wide = float.Parse(locations[2]);
				myMessage.height = float.Parse(locations[3]);
				
				//如果是手机设备
				if (Application.isMobilePlatform) {
					myMessage.wide = myMessage.wide/5000f;
					myMessage.height = myMessage.height/2300f;
					
				}else{
					myMessage.wide = myMessage.wide/1500f;
					myMessage.height = myMessage.height/2200f;
					
				}
				
				myMessage.locationX = myMessage.locationX/1000f;
				myMessage.locationY = myMessage.locationY/1000f ;
				if(myMessage.locationX<0.3){
					myMessage.locationX = myMessage.locationX + 0.125f;
				}
				
				//print(myMessage.wide);
				
				StartCoroutine("NewTarget");
			}
			else{
				//识别不出物体
				Debug.Log("识别不出物体 ");
				canTakePhotos = true;
				if (mQualityDialog)
				{
					mQualityDialog.gameObject.SetActive(true); //设置提示面板的显示
				}
			}
        }  
	}
	
	
	
	//创建新的Target
	public void NewTarget(){
		
		//修改文字
		title.GetComponent<TextMesh>().text = myMessage.className;
		info.GetComponent<TextMesh>().text = myMessage.information;
		
		//改变物体的显示大小
		//Cube.transform.localScale =new Vector3(myMessage.wide,myMessage.height,myMessage.wide);  //宽度、高度、厚度 
		Cube.transform.localScale =new Vector3(0.25f,0.25f,0.25f);  //宽度、高度、厚度 
		
		//改变物体的显示位置
		Cube.transform.localPosition =new Vector3(myMessage.locationX,0.015f,myMessage.locationY);  //左右、远近、上下
		  
		//调用外部类方法
		udteventhandler.BuildNewTarget();
		
	}
	
	

}

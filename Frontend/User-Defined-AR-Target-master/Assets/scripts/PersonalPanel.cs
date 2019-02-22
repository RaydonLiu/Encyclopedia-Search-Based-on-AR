using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersonalPanel : MonoBehaviour {

	private PersonalPanel myPersonalPanel; 

	// Use this for initialization
	void Start () {
		myPersonalPanel = FindObjectOfType<PersonalPanel>();
	}
	
	 public void OpenDialog()
    {
 
		 if ((myPersonalPanel.gameObject.active==true)){

            myPersonalPanel.gameObject.SetActive(false);
		}else{
			 myPersonalPanel.gameObject.SetActive(true);
		}
	}
}

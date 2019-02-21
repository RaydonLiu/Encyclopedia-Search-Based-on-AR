using UnityEngine;
using System.Collections;
using Vuforia;
using System.Threading;
using System.Collections.Generic;

public class GetImage : MonoBehaviour {

private Image.PIXEL_FORMAT m_PixelFormat = Image.PIXEL_FORMAT.RGB888;
private bool m_RegisteredFormat = false;
private bool m_LogInfo = false;
public Image image;
// Use this for initialization
void Start () {

}

// Update is called once per frame
void Update () {

}
public void OnTrackablesUpdated()
{
  Debug.Log ("Trackable updated called");
  if (!m_RegisteredFormat)
  {
   CameraDevice.Instance.SetFrameFormat(m_PixelFormat, true); //HERE IT GIVES THE ERROR
   m_RegisteredFormat = true;
  }
  if (m_LogInfo) {
   CameraDevice cam = CameraDevice.Instance;
   image = cam.GetCameraImage (m_PixelFormat);
   if (image == null) {
    Debug.Log (m_PixelFormat + " image is not available yet");
    //boxMesh.material.mainTexture = tx;

   } else {
    string s = m_PixelFormat + " image: \n";
    s += "  size: " + image.Width + "x" + image.Height + "\n";
    s += "  bufferSize: " + image.BufferWidth + "x" + image.BufferHeight + "\n";
    s += "  stride: " + image.Stride;
    Debug.Log (s);
   }
  }
}
}

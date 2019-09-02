using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


//나중에 시간나면 날짜 형식으로 바꾸기
//System.IO.File.WriteAllBytes(Application.dataPath + "/screenshots/screen" + 
//System.DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss") + ".png", bytes);

public class NY_Capture : MonoBehaviour {

    public GameObject mainFuncs;
    public GameObject subFuncs;

    //png파일로
    public int captureWidth = 1920;
    public int captureHeight = 1080;
    public string folder;
    public RenderTexture renderTexture;
    public Material mat;
    public AnimationCurve ac;

    public bool optimizeForManyScreenShots = true;


    Camera cam;
    AudioSource sound;
    private Rect rect;
    private Texture2D screenshot;
    private int counter = 0;
    private bool captureScreenshot = false;
    bool flash;


	// Use this for initialization
	void Start () {
        sound = GetComponent<AudioSource>();
        cam = GetComponent<Camera>();
        rect = new Rect(0, 0, captureWidth, captureHeight);
        screenshot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        renderTexture.width = captureWidth;
        renderTexture.height = captureHeight;
        renderTexture.depth = 24;

        gameObject.transform.position = new Vector3(GameManager.Instance.mainCamera.transform.position.x,
                                                    GameManager.Instance.mainCamera.transform.position.y,
                                                    GameManager.Instance.mainCamera.transform.position.z + 1);
    }

    //현철 추가
    void OnEnable(){

        

        mainFuncs.SetActive(false);
        subFuncs.SetActive(false);

        GetCameraSetting();

        gameObject.transform.position = new Vector3(GameManager.Instance.mainCamera.transform.position.x,
                                                GameManager.Instance.mainCamera.transform.position.y,
                                                GameManager.Instance.mainCamera.transform.position.z + 1);

    }

    void OnDisable(){
        mainFuncs.SetActive(true);
        subFuncs.SetActive(true);
    }

    void GetCameraSetting(){
        

        Camera cam = GetComponent<Camera>();
        Camera main = GameManager.Instance.mainCamera.GetComponent<Camera>();
        cam.clearFlags = main.clearFlags;
        cam.backgroundColor = main.backgroundColor;
    }
	

    public void CaptureScreenshot()
    {
        captureScreenshot = true;
    }

	// Update is called once per frame
	void Update () {

        // captureScreenshot |= OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger);

        // if (!flash && captureScreenshot)
        // {
        //     captureScreenshot = false;
        //     StartCoroutine(Flash());
        //     sound.Play();
        //     //cam.targetTexture = renderTexture;
        //     cam.Render();
        //     RenderTexture.active = renderTexture;
        //     screenshot.ReadPixels(rect, 0, 0);

        //     //cam.targetTexture = null;
        //     //RenderTexture.active = null;

        //     string filename = MakeFileName((int)rect.width, (int)rect.height);

        //     byte[] fileHeader = null;
        //     byte[] fileData = null;

        //     fileData = screenshot.EncodeToPNG();

        //     new System.Threading.Thread(() =>
        //     {
        //         var f = System.IO.File.Create(filename);
        //         if (fileHeader != null) f.Write(fileHeader, 0, fileHeader.Length);
        //         f.Write(fileData, 0, fileData.Length);
        //         f.Close();

        //     }).Start();

        //     if(optimizeForManyScreenShots == false)
        //     {
        //         //Destroy(renderTexture);
        //         //renderTexture = null;
        //         screenshot = null;
        //     }
        // }
        // //캡처하기 전에는 카메라를 움직이게 하기
        // else
        // {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger,OVRInput.Controller.RTouch))
            {
                print("get");
                gameObject.transform.parent = GameManager.Instance.RHand;
                
            }
            // else if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
            // {
            //     print("getL");
            //     gameObject.transform.parent = GameManager.Instance.LHand;
            // }
            else if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            {
                print("exit");
                gameObject.transform.parent = null;
            }
        // }
	}

    public void Capture(){

        captureScreenshot |= true;

        if (!flash && captureScreenshot)
        {
            captureScreenshot = false;
            StartCoroutine(Flash());
            sound.Play();
            //cam.targetTexture = renderTexture;
            cam.Render();
            RenderTexture.active = renderTexture;
            screenshot.ReadPixels(rect, 0, 0);

            //cam.targetTexture = null;
            //RenderTexture.active = null;

            string filename = MakeFileName((int)rect.width, (int)rect.height);

            byte[] fileHeader = null;
            byte[] fileData = null;

            fileData = screenshot.EncodeToPNG();

            new System.Threading.Thread(() =>
            {
                var f = System.IO.File.Create(filename);
                if (fileHeader != null) f.Write(fileHeader, 0, fileHeader.Length);
                f.Write(fileData, 0, fileData.Length);
                f.Close();

            }).Start();

            if(optimizeForManyScreenShots == false)
            {
                //Destroy(renderTexture);
                //renderTexture = null;
                screenshot = null;
            }
        }
    }

    IEnumerator Flash()
    {
        flash = true;
        float t = 0;

        Color color = Color.white;
        while (t < 1)
        {
            t += 0.08f;
            color = Color.Lerp(Color.black,Color.white, ac.Evaluate(t));
            mat.SetColor("_EmissionColor", color);
            yield return new WaitForEndOfFrame();
        }
        flash = false;
    }

    //create unique filename
    private string MakeFileName(int width, int height)
    {
        if (folder == null || folder.Length == 0)
        {
            folder = Application.dataPath;
            if (Application.isEditor)
            {
                var stringPath = folder + "/..";
                folder = Path.GetFullPath(stringPath);
            }
            folder += "/screenshots";

            System.IO.Directory.CreateDirectory(folder);

            string mask = string.Format("screen_{0}x{1}*.{2}", width, height, "png");
            counter = Directory.GetFiles(folder, mask, SearchOption.TopDirectoryOnly).Length;
        }

        var filename = string.Format("{0}/screen_{1}x{2}_{3}.{4}", folder, width, height, counter, "png");
        ++counter;

        return filename;
    }
}

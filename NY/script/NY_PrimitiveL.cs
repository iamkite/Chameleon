using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NY_PrimitiveL : MonoBehaviour
{

    //밖에서 넣어줘야함
    public GameObject primitiveMaker;
    public Transform output;
    private Transform hand;
    public GameObject transf;
    public GameObject toolbar;

    //조절
    public Color color = new Color(1, 1, 1);
    public float defaultsize = 0.06f;
    public float size;

    //소리
    AudioSource[] audios;
    OVRHapticsClip clip;

    //primitive 각각의 속성
    public GameObject curCube;
    public Material[] material;
    private Mesh mesh;
    //private MeshCollider mc;

    //animation
    public AnimationCurve ac;
    public float changeSpeed = 0.06f;
    public float changeSize = 1.2f;

    //changesize
    private GameObject curMaker;

    //size
    public float sizelimitup = 0.06f * 4f;
    public float sizelimitdown = 0.06f / 4f;
    float tempSize;
    bool sizeWaiting;

    public enum Shapes
    {
        Cube,
        Sphere,
        Cylinder,
        Cone,
        Triangularprism
    }
    public Shapes shapeof;

    public GameObject sphere;
    public GameObject cylinder;
    public GameObject cone;
    public GameObject cube;
    public GameObject triangularprism;


    public static NY_PrimitiveL Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    // Use this for initialization
    void Start()
    {
        hand = GameManager.Instance.LPoint;
        size = defaultsize;
        //tempSize = size;
        shapeof = Shapes.Cube;
        curMaker = Instantiate(primitiveMaker);
        audios = GetComponents<AudioSource>();
        clip = new OVRHapticsClip(audios[1].clip);
    }

    void OnEnable(){
    }

    void OnDisable(){
        //만약 비활되면 curcube를 지워라
        if(curCube){Destroy(curCube);}
    }

    // Update is called once per frame
    void Update()
    {
        // if (HC_Toolbox.isToolboxOn)
        // {

        // }
        if(transf.GetComponent<HC_Transform>().grab != HC_Transform.Grab.None || toolbar.GetComponent<HC_Toolbar>().isVisible || HC_Toolbox.isToolboxOn){

        }
        //아무것도 없을때 선택되어있는 모양 만들자
        else if (curCube == null)
        {
            //tempSize = size;
            GameObject p;
            switch (shapeof)
            {
                case Shapes.Sphere: p = Instantiate(sphere); break;
                case Shapes.Cylinder: p = Instantiate(cylinder); break;
                case Shapes.Cone: p = Instantiate(cone); break;
                case Shapes.Cube: p = Instantiate(cube); break;
                case Shapes.Triangularprism: p = Instantiate(triangularprism); break;
                default: p = Instantiate(cube); break;
            }
            curCube = p;
            p.layer = 0;
            p.transform.localScale = new Vector3(size, size, size);
            p.transform.position = hand.transform.position;
            p.transform.rotation = hand.transform.rotation * Quaternion.AngleAxis(-90, hand.transform.InverseTransformDirection(hand.transform.right));
            //p.transform.parent = output.transform;
            
            if (GameManager.Instance.view == GameManager.View.Wireframe)
            {
               /// 와이어 프레임일 경우, 매테리얼에 추가로 y_transition을 더해줘라.
               Material[] mts;
               mts = new Material[2];
               mts[0] = material[0];
               mts[1] = GameManager.Instance.mtWireFrame;
               p.GetComponent<MeshRenderer>().materials = mts;

            }
            else
            {
                p.GetComponent<MeshRenderer>().material = material[0];
            }


            mesh = p.GetComponent<MeshFilter>().mesh;

            Color[] ncolor = new Color[mesh.vertices.Length];
            //색넣어주기
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                ncolor[i] = GameManager.Instance.color;
            }
            mesh.colors = ncolor;
        }
        //트리거를 누르면 거기에 고정
        else if (curCube != null && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            //tempSize = size;
            audios[0].Play();
            
            curCube.layer = 9;

            curCube.transform.position = hand.transform.position;
            curCube.transform.rotation = hand.transform.rotation * Quaternion.AngleAxis(-90, hand.transform.InverseTransformDirection(hand.transform.right));

            curMaker.transform.position = hand.transform.position;
            curMaker.transform.rotation = hand.transform.rotation * Quaternion.AngleAxis(-90, hand.transform.InverseTransformDirection(hand.transform.right));
            curMaker.transform.localScale = new Vector3(curCube.transform.localScale.x, curCube.transform.localScale.y, curCube.transform.localScale.z);
        }
        //누르고있는 동안 사이즈 조절
        else if (curCube.layer == 9 && !sizeWaiting && curCube != null && OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {

            ChangeSize();

        }
        //최종 생성
        else if (curCube.layer == 9 && OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            OVRHaptics.LeftChannel.Preempt(clip);
            audios[1].Play();
            curCube.transform.parent = output.transform;
            NY_Undo.work.Push(0);
            NY_Undo.createdobj.Push(curCube);
            NY_Undo.redo = false;
            //잠깐 커졌다 돌아오는 애니메이션
            StartCoroutine(DrawingAnimation());
            curCube = null;
            mesh = null;
            if (output.transform.childCount > 1)
            {
                NY_StrokeR.Instance.set();
            }
        }
        //나머지 경우에는 curCube가 손을 따라가게 하자 + 크기 조절
        else
        {
            curCube.transform.position = hand.transform.position;
            curCube.transform.rotation = hand.transform.rotation * Quaternion.AngleAxis(-90, hand.transform.InverseTransformDirection(hand.transform.right));
            curCube.transform.localScale = new Vector3(size, size, size);
        }

    }


    void ChangeSize()
    {
        curMaker.transform.localScale = new Vector3(size, size, size);
        Vector3 pos = curMaker.transform.InverseTransformPoint(hand.transform.position);
        Vector3 newScale = new Vector3(size, size, size);
        Vector3 newPos = new Vector3(0, 0, 0);

        //world크기
        float posx = Mathf.Abs(pos.x * size);
        float posy = Mathf.Abs(pos.y * size);
        float posz = Mathf.Abs(pos.z * size);


        if (posx > size / 2)
        {
            newScale.x = posx + size / 2;
            newPos.x = (-size / 2 + (posx + size / 2) / 2) / size * pos.x / Mathf.Abs(pos.x);
        }

        if (posy > size / 2)
        {
            newScale.y = posy + size / 2;
            newPos.y = (-size / 2 + (posy + size / 2) / 2) / size * pos.y / Mathf.Abs(pos.y);
        }


        if (posz > size / 2)
        {
            newScale.z = posz + size / 2;
            newPos.z = (-size / 2 + (posz + size / 2) / 2) / size * pos.z / Mathf.Abs(pos.z);
        }


        curCube.transform.position = curMaker.transform.TransformPoint(newPos);
        curCube.transform.localScale = newScale;
    }

    IEnumerator DrawingAnimation()
    {
        float t = 0;
        Transform temp = curCube.GetComponent<Transform>();
        float tX = temp.localScale.x;
        float tY = temp.localScale.y;
        float tZ = temp.localScale.z;

        while (temp != null && t < 1)
        {
            t += changeSpeed;

            temp.localScale = Vector3.Lerp(new Vector3(tX, tY, tZ), new Vector3(tX, tY, tZ) * changeSize, ac.Evaluate(t));
            yield return new WaitForEndOfFrame();
        }
    }
}
